using System.Text.Json;
using System.Text.Json.Nodes;
using CommonOperations.Constants.Common;
using CommonOperations.Security;
using Domian.Entities.AppUsers;
using Domian.Entities.Gamification;
using Domian.Entities.Learning;
using Domian.Entities.Practice;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Seed
{
    /// <summary>
    /// Fills an empty database with the starter catalog (courses, lessons, problems, cosmetics, quotes)
    /// from Seed/Data/*.json, exported from the frontend mocks. Every set is only seeded while its table
    /// is empty, so content edited by admins is never overwritten.
    /// </summary>
    public class DbSeeder
    {
        private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<DbSeeder> _logger;
        private readonly string _dataDir = Path.Combine(AppContext.BaseDirectory, "Seed", "Data");

        public DbSeeder(AppDbContext db, IConfiguration config, ILogger<DbSeeder> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            await SeedCoursesAsync();
            await SeedLearningPathsAsync();
            await SeedProblemsAsync();
            await SeedCosmeticsAsync();
            await SeedQuotesAsync();
            await SeedAdminAsync();
        }

        private async Task SeedCoursesAsync()
        {
            if (await _db.Courses.AnyAsync())
            {
                return;
            }

            foreach (var source in Read<List<CourseSeed>>("courses.json"))
            {
                var course = new Course
                {
                    Slug = source.Slug,
                    Name = source.Name,
                    Categories = string.Join(',', source.Categories),
                    LearnerCount = source.Learners,
                    InvertOnDark = source.InvertOnDark,
                    Order = source.Order,
                    Sections = source.Sections.Select(section => new CourseSection
                    {
                        Number = section.Number,
                        Title = section.Title,
                        Description = section.Description,
                        LessonCount = section.LessonCount
                    }).ToList(),
                    MiniCourses = source.MoreCourses.Select((mini, i) => new MiniCourse
                    {
                        Key = mini.Id,
                        Title = mini.Title,
                        Description = mini.Description,
                        LessonCount = mini.LessonCount,
                        Certificate = mini.Certificate,
                        Tags = string.Join(',', mini.Tags),
                        Order = i
                    }).ToList()
                };

                // Authored chapters belong to section 1; later sections are filled in by admins.
                course.Sections.First(section => section.Number == 1).Chapters = source.Chapters.Select(chapter => new Chapter
                {
                    Key = chapter.Key,
                    Title = chapter.Title,
                    Order = chapter.Order,
                    Nodes = chapter.Nodes.Select(node => new JourneyNode
                    {
                        Key = node.Key,
                        Title = node.Title,
                        Kind = node.Kind,
                        Xp = node.Xp,
                        Order = node.Order,
                        Lesson = node.Lesson == null ? null : new Lesson
                        {
                            Summary = node.Lesson.Summary,
                            EditorLanguage = node.Lesson.EditorLanguage,
                            FileName = node.Lesson.FileName,
                            ContentJson = node.Lesson.Content.ToJsonString(),
                            ChallengeJson = node.Lesson.Challenge.ToJsonString(),
                            ReferenceJson = node.Lesson.Reference.ToJsonString(),
                            Solution = node.Lesson.Solution,
                            Tests = node.Lesson.Tests.Select((test, t) => new LessonTest
                            {
                                Key = test.Id,
                                Label = test.Label,
                                ExpectedOutput = test.ExpectedOutput,
                                Order = t
                            }).ToList()
                        }
                    }).ToList()
                }).ToList();

                _db.Courses.Add(course);
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} courses.", await _db.Courses.CountAsync());
        }

        private async Task SeedLearningPathsAsync()
        {
            if (await _db.LearningPaths.AnyAsync())
            {
                return;
            }

            var courses = await _db.Courses.ToDictionaryAsync(course => course.Slug, course => course.Id);
            foreach (var source in Read<List<PathSeed>>("learning-paths.json"))
            {
                _db.LearningPaths.Add(new LearningPath
                {
                    Slug = source.Id,
                    Title = source.Title,
                    Summary = source.Summary,
                    Order = source.Order,
                    Courses = source.Courses
                        .Where(courses.ContainsKey)
                        .Select((slug, i) => new LearningPathCourse { CourseId = courses[slug], Order = i })
                        .ToList()
                });
            }
            await _db.SaveChangesAsync();
        }

        private async Task SeedProblemsAsync()
        {
            if (await _db.Problems.AnyAsync())
            {
                return;
            }

            foreach (var source in Read<List<ProblemSeed>>("problems.json"))
            {
                var detail = source.Detail;
                var tests = detail["tests"]?.AsArray() ?? new JsonArray();
                // Title, difficulty and tests live in their own columns/table.
                detail.Remove("tests");
                detail.Remove("title");
                detail.Remove("difficulty");

                _db.Problems.Add(new Problem
                {
                    Key = source.Key,
                    Kind = source.Kind,
                    Number = source.Number,
                    Title = source.Title,
                    Topic = source.Topic,
                    Difficulty = source.Difficulty,
                    Language = source.Language,
                    DetailJson = detail.ToJsonString(),
                    Tests = tests.Select((test, i) => new ProblemTest
                    {
                        Key = test!["id"]!.GetValue<string>(),
                        Args = test["args"]!.GetValue<string>(),
                        Expected = test["expected"]!.GetValue<string>(),
                        Order = i
                    }).ToList()
                });
            }
            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} problems.", await _db.Problems.CountAsync());
        }

        private async Task SeedCosmeticsAsync()
        {
            if (await _db.CosmeticItems.AnyAsync())
            {
                return;
            }

            var source = Read<CosmeticsSeed>("cosmetics.json");
            _db.CosmeticItems.AddRange(source.Items.Select((item, i) => new CosmeticItem
            {
                Key = item.Id,
                Tab = item.Tab,
                Kind = item.Kind,
                Name = item.Name,
                Description = item.Description,
                Art = item.Art,
                Rarity = item.Rarity,
                UnlockMetric = item.Unlock?.Metric,
                UnlockTarget = item.Unlock?.Target ?? 0,
                Order = i
            }));
            await _db.SaveChangesAsync();
        }

        private async Task SeedQuotesAsync()
        {
            if (await _db.DailyQuotes.AnyAsync())
            {
                return;
            }

            var source = Read<LevelsSeed>("levels.json");
            _db.DailyQuotes.AddRange(source.Quotes.Select((text, i) => new DailyQuote
            {
                Text = text,
                Author = "Bit, Coddify's AI tutor",
                Likes = 600 + i * 37 % 400
            }));
            await _db.SaveChangesAsync();
        }

        /// <summary>Creates the first admin from Seed:AdminEmail / Seed:AdminPassword (user-secrets), if set.</summary>
        private async Task SeedAdminAsync()
        {
            var email = _config["Seed:AdminEmail"]?.Trim().ToLowerInvariant();
            var password = _config["Seed:AdminPassword"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)
                || await _db.AppUsers.AnyAsync(user => user.Email == email))
            {
                return;
            }

            _db.AppUsers.Add(new AppUser
            {
                Email = email,
                UserName = "admin",
                FullName = "Coddify Admin",
                Password = PasswordHasher.Hash(password),
                Role = Roles.Admin,
                IsEmailVerified = true,
                OnboardingCompleted = true
            });
            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded admin account {Email}.", email);
        }

        private T Read<T>(string file) =>
            JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(_dataDir, file)), Json)
            ?? throw new InvalidOperationException($"Seed file {file} is empty.");

        // ---- seed file shapes ----

        private sealed class CourseSeed
        {
            public string Slug { get; set; } = "";
            public string Name { get; set; } = "";
            public List<string> Categories { get; set; } = new();
            public int Learners { get; set; }
            public bool InvertOnDark { get; set; }
            public int Order { get; set; }
            public List<SectionSeed> Sections { get; set; } = new();
            public List<ChapterSeed> Chapters { get; set; } = new();
            public List<MiniCourseSeed> MoreCourses { get; set; } = new();
        }

        private sealed class SectionSeed
        {
            public int Number { get; set; }
            public string Title { get; set; } = "";
            public string Description { get; set; } = "";
            public int LessonCount { get; set; }
        }

        private sealed class ChapterSeed
        {
            public string Key { get; set; } = "";
            public int Order { get; set; }
            public string Title { get; set; } = "";
            public List<NodeSeed> Nodes { get; set; } = new();
        }

        private sealed class NodeSeed
        {
            public string Key { get; set; } = "";
            public int Order { get; set; }
            public string Title { get; set; } = "";
            public string Kind { get; set; } = "";
            public int Xp { get; set; }
            public LessonSeed? Lesson { get; set; }
        }

        private sealed class LessonSeed
        {
            public string Summary { get; set; } = "";
            public string EditorLanguage { get; set; } = "";
            public string FileName { get; set; } = "";
            public JsonArray Content { get; set; } = new();
            public JsonObject Challenge { get; set; } = new();
            public string Solution { get; set; } = "";
            public List<TestSeed> Tests { get; set; } = new();
            public JsonArray Reference { get; set; } = new();
        }

        private sealed class TestSeed
        {
            public string Id { get; set; } = "";
            public string Label { get; set; } = "";
            public string? ExpectedOutput { get; set; }
        }

        private sealed class MiniCourseSeed
        {
            public string Id { get; set; } = "";
            public string Title { get; set; } = "";
            public string Description { get; set; } = "";
            public int LessonCount { get; set; }
            public bool Certificate { get; set; }
            public List<string> Tags { get; set; } = new();
        }

        private sealed class PathSeed
        {
            public string Id { get; set; } = "";
            public string Title { get; set; } = "";
            public string Summary { get; set; } = "";
            public List<string> Courses { get; set; } = new();
            public int Order { get; set; }
        }

        private sealed class ProblemSeed
        {
            public string Key { get; set; } = "";
            public string Kind { get; set; } = "";
            public int Number { get; set; }
            public string Title { get; set; } = "";
            public string Topic { get; set; } = "";
            public string Difficulty { get; set; } = "";
            public string? Language { get; set; }
            public JsonObject Detail { get; set; } = new();
        }

        private sealed class CosmeticsSeed
        {
            public List<CosmeticSeed> Items { get; set; } = new();
        }

        private sealed class CosmeticSeed
        {
            public string Id { get; set; } = "";
            public string Tab { get; set; } = "";
            public string Kind { get; set; } = "";
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public string Art { get; set; } = "";
            public string Rarity { get; set; } = "common";
            public UnlockSeed? Unlock { get; set; }
        }

        private sealed class UnlockSeed
        {
            public string Metric { get; set; } = "";
            public int Target { get; set; }
        }

        private sealed class LevelsSeed
        {
            public List<string> Quotes { get; set; } = new();
        }
    }
}
