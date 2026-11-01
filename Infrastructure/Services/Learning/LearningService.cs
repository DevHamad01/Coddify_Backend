using System.Text.Json;
using Application.Interfaces.CodeExecution;
using Application.Interfaces.Common;
using Application.Interfaces.Gamification;
using Application.Interfaces.Learning;
using Application.VMs;
using Application.VMs.Learning;
using CommonOperations.Constants.Common;
using Domian.Entities.Learning;
using Domian.Entities.Progress;
using Infrastructure.Context;
using Infrastructure.Services.Gamification;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Learning
{
    public class LearningService : ILearningService
    {
        private const string Chest = "chest";

        private readonly AppDbContext _db;
        private readonly ICodeRunner _runner;
        private readonly IRewardService _rewards;
        private readonly IAppClock _clock;
        private readonly ILiveNotifier _live;

        public LearningService(AppDbContext db, ICodeRunner runner, IRewardService rewards, IAppClock clock, ILiveNotifier live)
        {
            _db = db;
            _runner = runner;
            _rewards = rewards;
            _clock = clock;
            _live = live;
        }

        // ---- catalog ----

        public async Task<ResponseVM> GetCoursesAsync()
        {
            var courses = await _db.Courses.AsNoTracking()
                .Where(x => x.IsPublished && !x.IsArchived)
                .OrderBy(x => x.Order)
                .ToListAsync();
            return ResponseVM.Ok(courses.Select(course => new CourseVM
            {
                Id = course.Slug,
                Name = course.Name,
                Categories = Split(course.Categories),
                Learners = course.LearnerCount,
                InvertOnDark = course.InvertOnDark
            }).ToList());
        }

        public async Task<ResponseVM> GetLearningPathsAsync()
        {
            var paths = await _db.LearningPaths.AsNoTracking()
                .Where(x => x.IsPublished && !x.IsArchived)
                .Include(x => x.Courses).ThenInclude(x => x.Course)
                .OrderBy(x => x.Order)
                .ToListAsync();
            return ResponseVM.Ok(paths.Select(path => new LearningPathVM
            {
                Id = path.Slug,
                Title = path.Title,
                Summary = path.Summary,
                Courses = path.Courses.OrderBy(x => x.Order)
                    .Where(x => x.Course is { IsPublished: true, IsArchived: false })
                    .Select(x => x.Course!.Slug).ToList()
            }).ToList());
        }

        public async Task<ResponseVM> GetSyllabusAsync(long userId, string courseSlug)
        {
            var course = await LoadCourseAsync(courseSlug);
            if (course == null)
            {
                return ResponseVM.NotFound("That course does not exist.");
            }

            var done = await CompletedNodeIdsAsync(userId, course);
            var enrolled = await _db.Enrollments.AnyAsync(x => x.UserId == userId && x.CourseId == course.Id);

            var sections = new List<CourseSectionVM>();
            var unlocked = true;
            foreach (var section in course.Sections)
            {
                var activities = Activities(section).ToList();
                var completed = activities.Count(node => done.Contains(node.Id));
                var finished = activities.Count > 0 && completed == activities.Count;
                string state;
                if (finished)
                {
                    state = "completed";
                }
                else if (unlocked)
                {
                    state = enrolled ? "in-progress" : "available";
                    unlocked = false;
                }
                else
                {
                    state = "locked";
                }

                sections.Add(new CourseSectionVM
                {
                    Number = section.Number,
                    Title = section.Title,
                    Description = section.Description,
                    LessonCount = Math.Max(section.LessonCount, activities.Count),
                    CompletedCount = completed,
                    State = state
                });
            }

            return ResponseVM.Ok(new CourseSyllabusVM
            {
                CourseId = course.Slug,
                CourseName = course.Name,
                Enrolled = enrolled,
                Sections = sections,
                MoreCourses = course.MiniCourses
                    .Where(x => x.IsPublished && !x.IsArchived)
                    .OrderBy(x => x.Order)
                    .Select(mini => new MiniCourseVM
                    {
                        Id = mini.Key,
                        Title = mini.Title,
                        Description = mini.Description,
                        LessonCount = mini.LessonCount,
                        Certificate = mini.Certificate,
                        Tags = Split(mini.Tags)
                    }).ToList()
            });
        }

        public async Task<ResponseVM> GetJourneyAsync(long userId, string courseSlug)
        {
            var course = await LoadCourseAsync(courseSlug);
            if (course == null)
            {
                return ResponseVM.NotFound("That course does not exist.");
            }

            var done = await CompletedNodeIdsAsync(userId, course);
            var section = CurrentSection(course, done);
            var currentAssigned = false;

            var chapters = section?.Chapters.Select((chapter, index) => new JourneyChapterVM
            {
                Id = chapter.Key,
                Number = index + 1,
                Title = chapter.Title,
                Nodes = chapter.Nodes.Select(node =>
                {
                    var state = "locked";
                    if (done.Contains(node.Id))
                    {
                        state = "completed";
                    }
                    else if (!currentAssigned)
                    {
                        state = "current";
                        currentAssigned = true;
                    }
                    return new JourneyNodeVM { Id = node.Key, Title = node.Title, Kind = node.Kind, State = state, Xp = node.Xp };
                }).ToList()
            }).ToList() ?? new List<JourneyChapterVM>();

            return ResponseVM.Ok(new JourneyVM
            {
                CourseId = course.Slug,
                CourseName = course.Name,
                SectionNumber = section?.Number ?? 1,
                SectionTitle = section?.Title ?? "",
                Chapters = chapters
            });
        }

        public async Task<ResponseVM> GetMyCoursesAsync(long userId)
        {
            var enrollments = await _db.Enrollments.AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.IsActive).ThenByDescending(x => x.LastOpenedAt)
                .ToListAsync();

            var result = new List<EnrolledCourseVM>();
            foreach (var enrollment in enrollments)
            {
                var course = await LoadCourseAsync(enrollment.CourseId);
                if (course == null)
                {
                    continue;
                }
                var done = await CompletedNodeIdsAsync(userId, course);
                var section = CurrentSection(course, done) ?? course.Sections.FirstOrDefault();
                var activities = section == null ? new List<JourneyNode>() : Activities(section).ToList();
                var total = Math.Max(section?.LessonCount ?? 0, activities.Count);
                result.Add(new EnrolledCourseVM
                {
                    CourseId = course.Slug,
                    CourseName = course.Name,
                    SectionNumber = section?.Number ?? 1,
                    SectionCount = course.Sections.Count,
                    SectionTitle = section?.Title ?? "",
                    Percent = total == 0 ? 0 : (int)Math.Round(activities.Count(node => done.Contains(node.Id)) * 100.0 / total),
                    Active = enrollment.IsActive
                });
            }
            return ResponseVM.Ok(result);
        }

        public async Task<ResponseVM> StartCourseAsync(long userId, string courseSlug)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(x => x.Slug == courseSlug && x.IsPublished && !x.IsArchived);
            if (course == null)
            {
                return ResponseVM.NotFound("That course does not exist.");
            }

            await EnrolAsync(userId, course.Id, makeActive: true);
            await _db.SaveChangesAsync();
            return await GetMyCoursesAsync(userId);
        }

        public async Task<ResponseVM> ClaimChestAsync(long userId, string nodeKey)
        {
            var node = await _db.JourneyNodes
                .Include(x => x.Chapter).ThenInclude(x => x!.Nodes)
                .FirstOrDefaultAsync(x => x.Key == nodeKey && x.IsPublished && !x.IsArchived);
            if (node == null || node.Kind != Chest)
            {
                return ResponseVM.NotFound("That chest does not exist.");
            }

            var chapterActivities = node.Chapter!.Nodes
                .Where(x => x.Kind != Chest && x.IsPublished && !x.IsArchived)
                .Select(x => x.Id)
                .ToList();
            var completed = await _db.NodeCompletions
                .Where(x => x.UserId == userId && chapterActivities.Contains(x.NodeId))
                .CountAsync();
            if (completed < chapterActivities.Count)
            {
                return ResponseVM.BadRequest(ErrorCodes.Locked, "Finish every lesson in this chapter to open the chest.");
            }

            if (await _db.NodeCompletions.AnyAsync(x => x.UserId == userId && x.NodeId == node.Id))
            {
                return ResponseVM.Ok(new ChestRewardVM { Gems = 0 }, "Chest already opened.");
            }

            _db.NodeCompletions.Add(new NodeCompletion { UserId = userId, NodeId = node.Id, CompletedAt = _clock.UtcNow });
            await _rewards.AddGemsAsync(userId, GamificationRules.ChestGems);
            await _db.SaveChangesAsync();
            return ResponseVM.Ok(new ChestRewardVM { Gems = GamificationRules.ChestGems }, "Chest opened!");
        }

        // ---- lessons ----

        public async Task<ResponseVM> GetLessonAsync(long userId, string lessonKey)
        {
            var node = await LoadLessonNodeAsync(lessonKey);
            if (node?.Lesson == null)
            {
                return ResponseVM.NotFound("That lesson does not exist.");
            }

            var course = await LoadCourseAsync(node.Chapter!.Section!.CourseId);
            var ordered = course == null ? new List<JourneyNode>() : course.Sections.SelectMany(Activities).ToList();
            var index = ordered.FindIndex(x => x.Id == node.Id);
            var lesson = node.Lesson;
            var challenge = JsonDocument.Parse(lesson.ChallengeJson).RootElement;

            return ResponseVM.Ok(new LessonVM
            {
                Id = node.Key,
                CourseId = course?.Slug ?? "",
                ChapterTitle = node.Chapter.Title,
                Title = node.Title,
                EditorLanguage = lesson.EditorLanguage,
                FileName = lesson.FileName,
                Summary = lesson.Summary,
                Content = JsonDocument.Parse(lesson.ContentJson).RootElement,
                Reference = JsonDocument.Parse(lesson.ReferenceJson).RootElement,
                Xp = node.Xp,
                NextLessonId = index >= 0 && index + 1 < ordered.Count ? ordered[index + 1].Key : null,
                Challenge = new LessonChallengeVM
                {
                    Level = ReadString(challenge, "level"),
                    Intro = ReadString(challenge, "intro"),
                    Steps = challenge.TryGetProperty("steps", out var steps)
                        ? steps.EnumerateArray().Select(step => step.GetString() ?? "").ToList()
                        : new List<string>(),
                    StarterCode = ReadString(challenge, "starterCode"),
                    // Shown by the frontend only after the first run.
                    Solution = lesson.Solution,
                    Tests = lesson.Tests.Where(test => !test.IsHidden).OrderBy(test => test.Order).Select(test => new TestCaseVM
                    {
                        Id = test.Key,
                        Label = test.Label,
                        ExpectedOutput = test.ExpectedOutput
                    }).ToList()
                }
            });
        }

        public async Task<ResponseVM> RunLessonAsync(long userId, string lessonKey, CodeVM model)
        {
            var node = await LoadLessonNodeAsync(lessonKey);
            if (node?.Lesson == null)
            {
                return ResponseVM.NotFound("That lesson does not exist.");
            }
            return ResponseVM.Ok(await JudgeAsync(node.Lesson, model.Code, includeHidden: false));
        }

        public async Task<ResponseVM> CompleteLessonAsync(long userId, string lessonKey, CompleteLessonVM model)
        {
            var node = await LoadLessonNodeAsync(lessonKey);
            if (node?.Lesson == null)
            {
                return ResponseVM.NotFound("That lesson does not exist.");
            }

            // Never trust the client: run every test (hidden ones too) again on the server.
            var result = await JudgeAsync(node.Lesson, model.Code, includeHidden: true);
            _db.LessonSubmissions.Add(new LessonSubmission
            {
                UserId = userId,
                NodeId = node.Id,
                Code = model.Code,
                Passed = result.Passed,
                SubmittedAt = _clock.UtcNow
            });

            if (!result.Passed)
            {
                await _db.SaveChangesAsync();
                return ResponseVM.BadRequest("tests_failed", "Some tests are still failing. Run your code and check the output.");
            }

            await EnrolAsync(userId, node.Chapter!.Section!.CourseId, makeActive: false);

            var alreadyDone = await _db.NodeCompletions.AnyAsync(x => x.UserId == userId && x.NodeId == node.Id);
            if (alreadyDone)
            {
                var stats = await _rewards.GetStatsAsync(userId);
                await _db.SaveChangesAsync();
                return ResponseVM.Ok(new LessonRewardVM { FirstTry = model.FirstTry, Streak = stats.Streak }, "Lesson reviewed.");
            }

            _db.NodeCompletions.Add(new NodeCompletion { UserId = userId, NodeId = node.Id, CompletedAt = _clock.UtcNow });
            var activity = await _rewards.RecordActivityAsync(userId, node.Xp, GamificationRules.LessonGems, model.FirstTry);
            await _db.SaveChangesAsync();
            await _live.XpChangedAsync(userId);

            return ResponseVM.Ok(new LessonRewardVM
            {
                XpEarned = node.Xp,
                GemsEarned = GamificationRules.LessonGems,
                FirstTry = model.FirstTry,
                StreakIncreased = activity.StreakIncreased,
                Streak = activity.Streak
            }, "Lesson complete!");
        }

        public async Task<ResponseVM> GetLessonSubmissionsAsync(long userId, string lessonKey)
        {
            var nodeId = await _db.JourneyNodes.Where(x => x.Key == lessonKey).Select(x => (long?)x.Id).FirstOrDefaultAsync();
            if (nodeId == null)
            {
                return ResponseVM.NotFound("That lesson does not exist.");
            }

            var submissions = await _db.LessonSubmissions.AsNoTracking()
                .Where(x => x.UserId == userId && x.NodeId == nodeId)
                .OrderByDescending(x => x.SubmittedAt)
                .Take(50)
                .Select(x => new SubmissionVM { Id = x.Id.ToString(), SubmittedAt = x.SubmittedAt, Passed = x.Passed, Code = x.Code })
                .ToListAsync();
            return ResponseVM.Ok(submissions);
        }

        // ---- helpers ----

        private async Task<RunResultVM> JudgeAsync(Lesson lesson, string code, bool includeHidden)
        {
            var run = await _runner.RunAsync(lesson.EditorLanguage, code);
            var checkedTests = lesson.Tests
                .Where(test => includeHidden || !test.IsHidden)
                .OrderBy(test => test.Order)
                .Select(test => (Test: test, Passed: run.Error == null
                    && (test.ExpectedOutput == null || Normalise(run.Output) == Normalise(test.ExpectedOutput))))
                .ToList();
            return new RunResultVM
            {
                Output = run.Output,
                Error = run.Error,
                // Hidden tests count towards passing but are never shown.
                Tests = checkedTests.Where(x => !x.Test.IsHidden)
                    .Select(x => new TestResultVM { TestId = x.Test.Key, Passed = x.Passed })
                    .ToList(),
                Passed = run.Error == null && checkedTests.All(x => x.Passed)
            };
        }

        private static string Normalise(string text) =>
            string.Join("\n", text.Replace("\r\n", "\n").Split('\n').Select(line => line.TrimEnd())).Trim();

        private async Task EnrolAsync(long userId, long courseId, bool makeActive)
        {
            var enrollments = await _db.Enrollments.Where(x => x.UserId == userId).ToListAsync();
            var enrollment = enrollments.FirstOrDefault(x => x.CourseId == courseId);
            if (enrollment == null)
            {
                enrollment = new Enrollment { UserId = userId, CourseId = courseId, StartedAt = _clock.UtcNow };
                _db.Enrollments.Add(enrollment);
                // The first course a learner starts becomes the active one.
                makeActive |= enrollments.All(x => !x.IsActive);
            }

            enrollment.LastOpenedAt = _clock.UtcNow;
            if (makeActive)
            {
                foreach (var other in enrollments)
                {
                    other.IsActive = false;
                }
                enrollment.IsActive = true;
            }
        }

        /// <summary>Published course tree (sections, chapters, nodes in order), archived parts left out.</summary>
        private Task<Course?> LoadCourseAsync(string slug) =>
            CourseTree().FirstOrDefaultAsync(x => x.Slug == slug && x.IsPublished && !x.IsArchived);

        private Task<Course?> LoadCourseAsync(long id) =>
            CourseTree().FirstOrDefaultAsync(x => x.Id == id && x.IsPublished && !x.IsArchived);

        private IQueryable<Course> CourseTree() => _db.Courses.AsNoTracking().AsSplitQuery()
            .Include(x => x.MiniCourses)
            .Include(x => x.Sections.Where(s => s.IsPublished && !s.IsArchived).OrderBy(s => s.Number))
                .ThenInclude(s => s.Chapters.Where(c => c.IsPublished && !c.IsArchived).OrderBy(c => c.Order))
                .ThenInclude(c => c.Nodes.Where(n => n.IsPublished && !n.IsArchived).OrderBy(n => n.Order));

        private Task<JourneyNode?> LoadLessonNodeAsync(string key) => _db.JourneyNodes.AsNoTracking()
            .Include(x => x.Lesson).ThenInclude(x => x!.Tests)
            .Include(x => x.Chapter).ThenInclude(x => x!.Section)
            .FirstOrDefaultAsync(x => x.Key == key && x.IsPublished && !x.IsArchived && x.Kind != Chest);

        private async Task<HashSet<long>> CompletedNodeIdsAsync(long userId, Course course)
        {
            var nodeIds = course.Sections.SelectMany(s => s.Chapters).SelectMany(c => c.Nodes).Select(n => n.Id).ToList();
            var done = await _db.NodeCompletions
                .Where(x => x.UserId == userId && nodeIds.Contains(x.NodeId))
                .Select(x => x.NodeId)
                .ToListAsync();
            return done.ToHashSet();
        }

        /// <summary>The first section that still has unfinished nodes (the last one when all are done).</summary>
        private static CourseSection? CurrentSection(Course course, HashSet<long> done) =>
            course.Sections.FirstOrDefault(s => s.Chapters.SelectMany(c => c.Nodes).Any(n => !done.Contains(n.Id))
                                                 && s.Chapters.Any(c => c.Nodes.Count > 0))
            ?? course.Sections.LastOrDefault(s => s.Chapters.Any(c => c.Nodes.Count > 0))
            ?? course.Sections.FirstOrDefault();

        private static IEnumerable<JourneyNode> Activities(CourseSection section) =>
            section.Chapters.SelectMany(c => c.Nodes).Where(n => n.Kind != Chest);

        private static List<string> Split(string csv) =>
            csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        private static string ReadString(JsonElement element, string name) =>
            element.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";
    }
}
