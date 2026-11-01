using System.Text.Json;
using Application.Interfaces.CodeExecution;
using Application.Interfaces.Common;
using Application.Interfaces.Gamification;
using Application.Interfaces.Practice;
using Application.VMs;
using Application.VMs.Practice;
using CommonOperations.Constants.Common;
using Domian.Entities.Practice;
using Infrastructure.Context;
using Infrastructure.Services.Gamification;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Practice
{
    public class PracticeService : IPracticeService
    {
        private const string Interview = "interview";
        private const string Challenge = "challenge";
        private static readonly string[] Difficulties = { "Beginner", "Easy", "Medium", "Hard" };

        private readonly AppDbContext _db;
        private readonly IProblemJudge _judge;
        private readonly IRewardService _rewards;
        private readonly IAppClock _clock;
        private readonly ILiveNotifier _live;

        public PracticeService(AppDbContext db, IProblemJudge judge, IRewardService rewards, IAppClock clock, ILiveNotifier live)
        {
            _db = db;
            _judge = judge;
            _rewards = rewards;
            _clock = clock;
            _live = live;
        }

        // ---- interview prep ----

        public async Task<ResponseVM> GetProblemsAsync(long userId, ProblemQueryVM query)
        {
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var status = await StatusMapAsync(userId);

            var problems = Published().Where(x => x.Kind == Interview);
            if (!string.IsNullOrWhiteSpace(query.Topic)) problems = problems.Where(x => x.Topic == query.Topic);
            if (!string.IsNullOrWhiteSpace(query.Difficulty)) problems = problems.Where(x => x.Difficulty == query.Difficulty);
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                problems = int.TryParse(search, out var number)
                    ? problems.Where(x => x.Number == number || x.Title.Contains(search))
                    : problems.Where(x => x.Title.Contains(search) || x.Topic.Contains(search));
            }

            var items = (await problems.Select(x => new { x.Id, x.Key, x.Number, x.Title, x.Topic, x.Difficulty }).ToListAsync())
                .Select(x => new InterviewProblemVM
                {
                    Id = x.Key,
                    Number = x.Number,
                    Title = x.Title,
                    Topic = x.Topic,
                    Difficulty = x.Difficulty,
                    Status = status.GetValueOrDefault(x.Id, "todo")
                })
                .Where(x => string.IsNullOrWhiteSpace(query.Status) || x.Status == query.Status);

            var descending = query.Direction == "desc";
            items = query.Sort switch
            {
                "title" => descending ? items.OrderByDescending(x => x.Title) : items.OrderBy(x => x.Title),
                "topic" => (descending ? items.OrderByDescending(x => x.Topic) : items.OrderBy(x => x.Topic)).ThenBy(x => x.Number),
                "difficulty" => (descending
                    ? items.OrderByDescending(x => Array.IndexOf(Difficulties, x.Difficulty))
                    : items.OrderBy(x => Array.IndexOf(Difficulties, x.Difficulty))).ThenBy(x => x.Number),
                _ => descending ? items.OrderByDescending(x => x.Number) : items.OrderBy(x => x.Number)
            };

            var all = items.ToList();
            return ResponseVM.Ok(new PageVM<InterviewProblemVM>
            {
                Items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Total = all.Count,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ResponseVM> GetTopicsAsync()
        {
            var topics = await Published().Where(x => x.Kind == Interview).Select(x => x.Topic).Distinct().OrderBy(x => x).ToListAsync();
            return ResponseVM.Ok(topics);
        }

        public async Task<ResponseVM> GetTodaySetAsync(long userId)
        {
            var status = await StatusMapAsync(userId);
            var problems = await Published().Where(x => x.Kind == Interview).OrderBy(x => x.Number).ToListAsync();
            var picks = problems.Where(x => status.GetValueOrDefault(x.Id) != "solved").Take(3).ToList();
            if (picks.Count < 3)
            {
                picks = problems.TakeLast(3).ToList();
            }
            if (picks.Count == 0)
            {
                return ResponseVM.NotFound("No interview problems yet.");
            }

            return ResponseVM.Ok(new InterviewSetVM
            {
                Title = picks[0].Title,
                Topic = picks[0].Topic,
                Difficulty = picks[0].Difficulty,
                Problems = picks.Select((x, i) => new InterviewProblemVM
                {
                    Id = x.Key,
                    Number = x.Number,
                    Title = x.Title,
                    Topic = x.Topic,
                    Difficulty = x.Difficulty,
                    Status = status.GetValueOrDefault(x.Id, "todo"),
                    Surprise = i == 2 ? true : null
                }).ToList()
            });
        }

        public async Task<ResponseVM> GetProgressAsync(long userId)
        {
            var solved = await _db.SolvedProblems
                .Where(x => x.UserId == userId)
                .Join(_db.Problems.Where(p => p.Kind == Interview), s => s.ProblemId, p => p.Id, (_, p) => p.Difficulty)
                .GroupBy(x => x)
                .Select(g => new { Difficulty = g.Key, Count = g.Count() })
                .ToListAsync();
            var stats = await _rewards.GetStatsAsync(userId);
            await _db.SaveChangesAsync();

            return ResponseVM.Ok(new InterviewProgressVM
            {
                Solved = Difficulties.ToDictionary(d => d, d => solved.FirstOrDefault(x => x.Difficulty == d)?.Count ?? 0),
                InterviewDate = stats.InterviewDate?.ToString("yyyy-MM-dd")
            });
        }

        public async Task<ResponseVM> SetInterviewDateAsync(long userId, InterviewDateVM model)
        {
            if (model.Date is { } date && date < _clock.Today)
            {
                return ResponseVM.BadRequest(ErrorCodes.Validation, "Pick a date from today onwards.");
            }
            var stats = await _rewards.GetStatsAsync(userId);
            stats.InterviewDate = model.Date;
            await _db.SaveChangesAsync();
            return await GetProgressAsync(userId);
        }

        // ---- challenges ----

        public async Task<ResponseVM> GetChallengesAsync(long userId, string[] languages, string[] difficulties, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var status = await StatusMapAsync(userId);

            var challenges = Published().Where(x => x.Kind == Challenge);
            if (languages.Length > 0) challenges = challenges.Where(x => languages.Contains(x.Language!));
            if (difficulties.Length > 0) challenges = challenges.Where(x => difficulties.Contains(x.Difficulty));

            var total = await challenges.CountAsync();
            var items = await challenges.OrderBy(x => x.Number)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new { x.Id, x.Key, x.Number, x.Title, x.Language, x.Difficulty })
                .ToListAsync();

            return ResponseVM.Ok(new PageVM<ChallengeVM>
            {
                Items = items.Select(x => new ChallengeVM
                {
                    Id = x.Key,
                    Number = x.Number,
                    Title = x.Title,
                    Language = x.Language ?? "",
                    Difficulty = x.Difficulty,
                    Status = status.GetValueOrDefault(x.Id, "todo")
                }).ToList(),
                Total = total,
                Page = page,
                PageSize = pageSize
            });
        }

        // ---- workspace ----

        public async Task<ResponseVM> GetProblemAsync(long userId, string problemKey)
        {
            var problem = await LoadAsync(problemKey);
            if (problem == null)
            {
                return ResponseVM.NotFound("That problem does not exist.");
            }

            var status = await StatusMapAsync(userId);
            var detail = JsonDocument.Parse(problem.DetailJson).RootElement;
            JsonElement Get(string name) => detail.TryGetProperty(name, out var value) ? value.Clone() : default;
            string GetString(string name) => detail.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";

            return ResponseVM.Ok(new ProblemDetailVM
            {
                Id = problem.Key,
                Title = problem.Title,
                Difficulty = problem.Difficulty,
                Topics = Get("topics"),
                Statement = Get("statement"),
                FunctionName = GetString("functionName"),
                Params = Get("params"),
                Returns = Get("returns"),
                Constraints = Get("constraints"),
                Examples = Get("examples"),
                Hints = Get("hints"),
                Solution = Get("solution"),
                Languages = Get("languages"),
                DefaultLanguage = GetString("defaultLanguage"),
                StarterCode = Get("starterCode"),
                Similar = await SimilarAsync(problem),
                Tests = problem.Tests.Where(x => !x.IsHidden).OrderBy(x => x.Order)
                    .Select(x => new ProblemTestVM { Id = x.Key, Args = x.Args, Expected = x.Expected }).ToList(),
                Status = status.GetValueOrDefault(problem.Id, "todo")
            });
        }

        public async Task<ResponseVM> RunAsync(long userId, string problemKey, ProblemCodeVM model)
        {
            var problem = await LoadAsync(problemKey);
            if (problem == null)
            {
                return ResponseVM.NotFound("That problem does not exist.");
            }
            return ResponseVM.Ok(await JudgeAsync(problem, model, includeHidden: false));
        }

        public async Task<ResponseVM> SubmitAsync(long userId, string problemKey, ProblemCodeVM model)
        {
            var problem = await LoadAsync(problemKey);
            if (problem == null)
            {
                return ResponseVM.NotFound("That problem does not exist.");
            }

            var result = await JudgeAsync(problem, model, includeHidden: true);
            _db.ProblemSubmissions.Add(new ProblemSubmission
            {
                UserId = userId,
                ProblemId = problem.Id,
                Language = model.Language,
                Code = model.Code,
                Passed = result.Passed,
                SubmittedAt = _clock.UtcNow
            });

            var firstSolve = result.Passed && !await _db.SolvedProblems.AnyAsync(x => x.UserId == userId && x.ProblemId == problem.Id);
            if (firstSolve)
            {
                _db.SolvedProblems.Add(new SolvedProblem { UserId = userId, ProblemId = problem.Id, SolvedAt = _clock.UtcNow });
                await _rewards.RecordActivityAsync(userId, GamificationRules.SolveXp, 0, perfect: false);
            }
            await _db.SaveChangesAsync();
            if (firstSolve)
            {
                await _live.XpChangedAsync(userId);
            }

            return ResponseVM.Ok(result, result.Passed ? "All tests passed!" : "Some tests failed.");
        }

        public async Task<ResponseVM> GetSubmissionsAsync(long userId, string problemKey)
        {
            var problemId = await _db.Problems.Where(x => x.Key == problemKey).Select(x => (long?)x.Id).FirstOrDefaultAsync();
            if (problemId == null)
            {
                return ResponseVM.NotFound("That problem does not exist.");
            }

            var submissions = await _db.ProblemSubmissions.AsNoTracking()
                .Where(x => x.UserId == userId && x.ProblemId == problemId)
                .OrderByDescending(x => x.SubmittedAt)
                .Take(50)
                .Select(x => new ProblemSubmissionVM
                {
                    Id = x.Id.ToString(),
                    SubmittedAt = x.SubmittedAt,
                    Language = x.Language,
                    Passed = x.Passed,
                    Code = x.Code
                })
                .ToListAsync();
            return ResponseVM.Ok(submissions);
        }

        // ---- helpers ----

        private IQueryable<Problem> Published() => _db.Problems.AsNoTracking().Where(x => x.IsPublished && !x.IsArchived);

        private Task<Problem?> LoadAsync(string key) =>
            Published().Include(x => x.Tests).FirstOrDefaultAsync(x => x.Key == key);

        /// <summary>Problem id -> "solved" | "in-progress" for this learner.</summary>
        private async Task<Dictionary<long, string>> StatusMapAsync(long userId)
        {
            var solved = await _db.SolvedProblems.Where(x => x.UserId == userId).Select(x => x.ProblemId).ToListAsync();
            var tried = await _db.ProblemSubmissions.Where(x => x.UserId == userId).Select(x => x.ProblemId).Distinct().ToListAsync();
            var map = tried.ToDictionary(id => id, _ => "in-progress");
            foreach (var id in solved)
            {
                map[id] = "solved";
            }
            return map;
        }

        private async Task<List<SimilarProblemVM>> SimilarAsync(Problem problem)
        {
            var others = problem.Kind == Interview
                ? Published().Where(x => x.Kind == Interview && x.Topic == problem.Topic && x.Id != problem.Id)
                : Published().Where(x => x.Kind == Challenge && x.Language == problem.Language && x.Id != problem.Id);
            var count = await others.CountAsync();
            var skip = problem.Kind == Interview || count <= 3 ? 0 : problem.Number % (count - 3);
            return await others.OrderBy(x => x.Number).Skip(skip).Take(3)
                .Select(x => new SimilarProblemVM { Id = x.Key, Title = x.Title, Difficulty = x.Difficulty, Topic = x.Topic })
                .ToListAsync();
        }

        private async Task<ProblemRunResultVM> JudgeAsync(Problem problem, ProblemCodeVM model, bool includeHidden)
        {
            var detail = JsonDocument.Parse(problem.DetailJson).RootElement;
            var functionName = detail.TryGetProperty("functionName", out var fn) ? fn.GetString() ?? "" : "";
            var starter = detail.TryGetProperty("starterCode", out var starters) && starters.TryGetProperty(model.Language, out var s)
                ? s.GetString() ?? ""
                : "";

            var tests = problem.Tests.Where(x => includeHidden || !x.IsHidden).OrderBy(x => x.Order).ToList();
            var (results, error) = await _judge.JudgeAsync(
                model.Language, functionName, model.Code, starter,
                tests.Select(x => new JudgeTest(x.Key, x.Args, x.Expected)).ToList());

            var hidden = tests.Where(x => x.IsHidden).Select(x => x.Key).ToHashSet();
            return new ProblemRunResultVM
            {
                Results = results.Where(x => !hidden.Contains(x.Key))
                    .Select(x => new ProblemTestResultVM { TestId = x.Key, Output = x.Output, Passed = x.Passed }).ToList(),
                HiddenTotal = hidden.Count,
                HiddenPassed = results.Count(x => hidden.Contains(x.Key) && x.Passed),
                Passed = error == null && results.Count > 0 && results.All(x => x.Passed),
                Error = error
            };
        }
    }
}
