using System.Text.Json;
using Application.Interfaces.Admin;
using Application.Interfaces.Common;
using Application.VMs;
using Application.VMs.Admin;
using CommonOperations.Constants.Common;
using Domian.Entities.Common;
using Domian.Entities.Gamification;
using Domian.Entities.Learning;
using Domian.Entities.Practice;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Admin
{
    public class AdminContentService : IAdminContentService
    {
        private readonly AppDbContext _db;
        private readonly IAppClock _clock;

        public AdminContentService(AppDbContext db, IAppClock clock)
        {
            _db = db;
            _clock = clock;
        }

        // ---- dashboard ----

        public async Task<ResponseVM> GetDashboardAsync()
        {
            var today = _clock.Today;
            var popular = await _db.Enrollments
                .GroupBy(x => x.CourseId)
                .Select(g => new { CourseId = g.Key, Learners = g.Count() })
                .OrderByDescending(x => x.Learners)
                .Take(5)
                .Join(_db.Courses, x => x.CourseId, c => c.Id, (x, c) => new { course = c.Slug, c.Name, x.Learners })
                .ToListAsync();

            return ResponseVM.Ok(new
            {
                learners = await _db.AppUsers.CountAsync(x => !x.IsDeleted && x.Role == Roles.Learner),
                activeToday = await _db.ActivityDays.CountAsync(x => x.Day == today),
                lessonsCompleted = await _db.NodeCompletions
                    .Join(_db.JourneyNodes.Where(n => n.Kind != "chest"), c => c.NodeId, n => n.Id, (c, _) => c.Id)
                    .CountAsync(),
                problemsSolved = await _db.SolvedProblems.CountAsync(),
                courses = await _db.Courses.CountAsync(x => !x.IsArchived),
                problems = await _db.Problems.CountAsync(x => !x.IsArchived),
                popularCourses = popular
            });
        }

        // ---- courses ----

        public async Task<ResponseVM> GetCoursesAsync()
        {
            var courses = await _db.Courses.AsNoTracking()
                .OrderBy(x => x.Order)
                .Select(x => new
                {
                    x.Id, x.Slug, x.Name, x.Categories, x.LearnerCount, x.InvertOnDark, x.Order, x.IsPublished, x.IsArchived,
                    sections = x.Sections.Count,
                    enrolled = _db.Enrollments.Count(e => e.CourseId == x.Id)
                })
                .ToListAsync();
            return ResponseVM.Ok(courses);
        }

        public async Task<ResponseVM> GetCourseTreeAsync(long courseId)
        {
            var course = await _db.Courses.AsNoTracking().AsSplitQuery()
                .Include(x => x.Sections.OrderBy(s => s.Number))
                    .ThenInclude(s => s.Chapters.OrderBy(c => c.Order))
                    .ThenInclude(c => c.Nodes.OrderBy(n => n.Order))
                .FirstOrDefaultAsync(x => x.Id == courseId);
            if (course == null)
            {
                return ResponseVM.NotFound("Course not found.");
            }

            return ResponseVM.Ok(new
            {
                course.Id, course.Slug, course.Name, course.IsPublished, course.IsArchived,
                sections = course.Sections.Select(s => new
                {
                    s.Id, s.Number, s.Title, s.Description, s.LessonCount, s.IsPublished, s.IsArchived,
                    chapters = s.Chapters.Select(c => new
                    {
                        c.Id, c.Key, c.Title, c.Order, c.IsPublished, c.IsArchived,
                        nodes = c.Nodes.Select(n => new { n.Id, n.Key, n.Title, n.Kind, n.Xp, n.Order, n.IsPublished, n.IsArchived })
                    })
                })
            });
        }

        public async Task<ResponseVM> CreateCourseAsync(CourseUpsertVM model)
        {
            if (await _db.Courses.AnyAsync(x => x.Slug == model.Slug))
            {
                return Conflict("slug", "A course with this id already exists.");
            }

            var course = new Course { IsPublished = false };
            Apply(course, model);
            _db.Courses.Add(course);
            await _db.SaveChangesAsync();
            return ResponseVM.Ok(new { course.Id }, "Course created as a draft.");
        }

        public async Task<ResponseVM> UpdateCourseAsync(long courseId, CourseUpsertVM model)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(x => x.Id == courseId);
            if (course == null)
            {
                return ResponseVM.NotFound("Course not found.");
            }
            if (course.Slug != model.Slug)
            {
                if (await _db.Enrollments.AnyAsync(x => x.CourseId == courseId))
                {
                    return Conflict("slug", "Learners are enrolled in this course, so its id cannot change.");
                }
                if (await _db.Courses.AnyAsync(x => x.Slug == model.Slug && x.Id != courseId))
                {
                    return Conflict("slug", "A course with this id already exists.");
                }
            }

            Apply(course, model);
            course.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return ResponseVM.Ok(new { course.Id }, "Course saved.");
        }

        private static void Apply(Course course, CourseUpsertVM model)
        {
            course.Slug = model.Slug;
            course.Name = model.Name.Trim();
            course.Categories = string.Join(',', model.Categories.Select(x => x.Trim()).Where(x => x.Length > 0));
            course.LearnerCount = model.LearnerCount;
            course.InvertOnDark = model.InvertOnDark;
            course.Order = model.Order;
        }

        // ---- sections and chapters ----

        public async Task<ResponseVM> CreateSectionAsync(long courseId, SectionUpsertVM model)
        {
            if (!await _db.Courses.AnyAsync(x => x.Id == courseId))
            {
                return ResponseVM.NotFound("Course not found.");
            }
            if (await _db.CourseSections.AnyAsync(x => x.CourseId == courseId && x.Number == model.Number))
            {
                return Conflict("number", $"Section {model.Number} already exists in this course.");
            }

            var section = new CourseSection
            {
                CourseId = courseId,
                Number = model.Number,
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                LessonCount = model.LessonCount,
                IsPublished = false
            };
            _db.CourseSections.Add(section);
            await _db.SaveChangesAsync();
            return ResponseVM.Ok(new { section.Id }, "Section created as a draft.");
        }

        public async Task<ResponseVM> UpdateSectionAsync(long sectionId, SectionUpsertVM model)
        {
            var section = await _db.CourseSections.FirstOrDefaultAsync(x => x.Id == sectionId);
            if (section == null)
            {
                return ResponseVM.NotFound("Section not found.");
            }
            if (section.Number != model.Number
                && await _db.CourseSections.AnyAsync(x => x.CourseId == section.CourseId && x.Number == model.Number))
            {
                return Conflict("number", $"Section {model.Number} already exists in this course.");
            }

            section.Number = model.Number;
            section.Title = model.Title.Trim();
            section.Description = model.Description.Trim();
            section.LessonCount = model.LessonCount;
            await _db.SaveChangesAsync();
            return ResponseVM.Ok(new { section.Id }, "Section saved.");
        }

        public async Task<ResponseVM> CreateChapterAsync(long sectionId, ChapterUpsertVM model)
        {
            if (!await _db.CourseSections.AnyAsync(x => x.Id == sectionId))
            {
                return ResponseVM.NotFound("Section not found.");
            }
            if (await _db.Chapters.AnyAsync(x => x.SectionId == sectionId && x.Key == model.Key))
            {
                return Conflict("key", "A chapter with this id already exists in this section.");
            }

            var chapter = new Chapter { SectionId = sectionId, Key = model.Key, Title = model.Title.Trim(), Order = model.Order, IsPublished = false };
            _db.Chapters.Add(chapter);
            await _db.SaveChangesAsync();
            return ResponseVM.Ok(new { chapter.Id }, "Chapter created as a draft.");
        }

        public async Task<ResponseVM> UpdateChapterAsync(long chapterId, ChapterUpsertVM model)
        {
            var chapter = await _db.Chapters.FirstOrDefaultAsync(x => x.Id == chapterId);
            if (chapter == null)
            {
                return ResponseVM.NotFound("Chapter not found.");
            }
            if (chapter.Key != model.Key && await _db.Chapters.AnyAsync(x => x.SectionId == chapter.SectionId && x.Key == model.Key))
            {
                return Conflict("key", "A chapter with this id already exists in this section.");
            }

            chapter.Key = model.Key;
            chapter.Title = model.Title.Trim();
            chapter.Order = model.Order;
            await _db.SaveChangesAsync();
            return ResponseVM.Ok(new { chapter.Id }, "Chapter saved.");
        }

        public async Task<ResponseVM> ReorderChaptersAsync(long sectionId, ReorderVM model)
        {
            var chapters = await _db.Chapters.Where(x => x.SectionId == sectionId).ToListAsync();
            if (!SameSet(chapters.Select(x => x.Id), model.Ids))
            {
                return ResponseVM.BadRequest(ErrorCodes.Validation, "Send every chapter of the section exactly once.");
            }
            foreach (var chapter in chapters)
            {
                chapter.Order = model.Ids.IndexOf(chapter.Id);
            }
            await _db.SaveChangesAsync();
            return ResponseVM.Ok(null, "Chapters reordered.");
        }

        // ---- nodes and lessons ----

        public async Task<ResponseVM> GetNodeAsync(long nodeId)
        {
            var node = await _db.JourneyNodes.AsNoTracking()
                .Include(x => x.Lesson).ThenInclude(x => x!.Tests)
                .FirstOrDefaultAsync(x => x.Id == nodeId);
            if (node == null)
            {
                return ResponseVM.NotFound("Node not found.");
            }

            var lesson = node.Lesson;
            return ResponseVM.Ok(new
            {
                node.Id, node.ChapterId, node.Key, node.Title, node.Kind, node.Xp, node.Order, node.IsPublished, node.IsArchived,
                completions = await _db.NodeCompletions.CountAsync(x => x.NodeId == nodeId),
                lesson = lesson == null ? null : new
                {
                    lesson.Summary,
                    lesson.EditorLanguage,
                    lesson.FileName,
                    content = JsonDocument.Parse(lesson.ContentJson).RootElement,
                    challenge = JsonDocument.Parse(lesson.ChallengeJson).RootElement,
                    lesson.Solution,
                    reference = JsonDocument.Parse(lesson.ReferenceJson).RootElement,
                    tests = lesson.Tests.OrderBy(t => t.Order).Select(t => new { t.Key, t.Label, t.ExpectedOutput, t.IsHidden })
                }
            });
        }

        public async Task<ResponseVM> CreateNodeAsync(long chapterId, NodeUpsertVM model)
        {
            if (!await _db.Chapters.AnyAsync(x => x.Id == chapterId))
            {
                return ResponseVM.NotFound("Chapter not found.");
            }
            if (await _db.JourneyNodes.AnyAsync(x => x.Key == model.Key))
            {
                return Conflict("key", "A node with this id already exists.");
            }
            var invalid = ValidateLesson(model);
            if (invalid != null)
            {
                return invalid;
            }

            var node = new JourneyNode { ChapterId = chapterId, IsPublished = false };
            ApplyNode(node, model);
            _db.JourneyNodes.Add(node);
            await _db.SaveChangesAsync();
            return ResponseVM.Ok(new { node.Id }, "Node created as a draft.");
        }

        public async Task<ResponseVM> UpdateNodeAsync(long nodeId, NodeUpsertVM model)
        {
            var node = await _db.JourneyNodes
                .Include(x => x.Lesson).ThenInclude(x => x!.Tests)
                .FirstOrDefaultAsync(x => x.Id == nodeId);
            if (node == null)
            {
                return ResponseVM.NotFound("Node not found.");
            }

            var used = await _db.NodeCompletions.AnyAsync(x => x.NodeId == nodeId);
            if (node.Key != model.Key)
            {
                if (used)
                {
                    return Conflict("key", "Learners have finished this node, so its id cannot change.");
                }
                if (await _db.JourneyNodes.AnyAsync(x => x.Key == model.Key && x.Id != nodeId))
                {
                    return Conflict("key", "A node with this id already exists.");
                }
            }
            if (used && node.Kind != model.Kind)
            {
                return Conflict("kind", "Learners have finished this node, so its kind cannot change.");
            }
            var invalid = ValidateLesson(model);
            if (invalid != null)
            {
                return invalid;
            }

            if (node.Lesson != null)
            {
                _db.LessonTests.RemoveRange(node.Lesson.Tests);
                node.Lesson.Tests.Clear();
            }
            ApplyNode(node, model);
            await _db.SaveChangesAsync();
            return ResponseVM.Ok(new { node.Id }, "Node saved.");
        }

        public async Task<ResponseVM> ReorderNodesAsync(long chapterId, ReorderVM model)
        {
            var nodes = await _db.JourneyNodes.Where(x => x.ChapterId == chapterId).ToListAsync();
            if (!SameSet(nodes.Select(x => x.Id), model.Ids))
            {
                return ResponseVM.BadRequest(ErrorCodes.Validation, "Send every node of the chapter exactly once.");
            }
            foreach (var node in nodes)
            {
                node.Order = model.Ids.IndexOf(node.Id);
            }
            await _db.SaveChangesAsync();
            return ResponseVM.Ok(null, "Nodes reordered.");
        }

        private static ResponseVM? ValidateLesson(NodeUpsertVM model)
        {
            if (model.Kind == "chest")
            {
                return null;
            }
            if (model.Lesson == null)
            {
                return Invalid("lesson", "Lessons, quizzes and practice nodes need lesson content.");
            }
            if (model.Lesson.Content.ValueKind != JsonValueKind.Array)
            {
                return Invalid("lesson.content", "Content must be a list of blocks.");
            }
            if (model.Lesson.Reference.ValueKind is not (JsonValueKind.Array or JsonValueKind.Undefined or JsonValueKind.Null))
            {
                return Invalid("lesson.reference", "Reference must be a list of cards.");
            }
            if (model.Lesson.Tests.Count == 0)
            {
                return Invalid("lesson.tests", "Add at least one test.");
            }
            if (model.Lesson.Tests.Select(t => t.Key).Distinct().Count() != model.Lesson.Tests.Count)
            {
                return Invalid("lesson.tests", "Test ids must be unique.");
            }
            return null;
        }

        private static void ApplyNode(JourneyNode node, NodeUpsertVM model)
        {
            node.Key = model.Key;
            node.Title = model.Title.Trim();
            node.Kind = model.Kind;
            node.Xp = model.Kind == "chest" ? 0 : model.Xp;
            node.Order = model.Order;

            if (model.Kind == "chest" || model.Lesson == null)
            {
                return;
            }

            var source = model.Lesson;
            node.Lesson ??= new Lesson();
            node.Lesson.Summary = source.Summary.Trim();
            node.Lesson.EditorLanguage = source.EditorLanguage;
            node.Lesson.FileName = source.FileName;
            node.Lesson.ContentJson = source.Content.GetRawText();
            node.Lesson.ChallengeJson = JsonSerializer.Serialize(new
            {
                level = source.Challenge.Level,
                intro = source.Challenge.Intro,
                steps = source.Challenge.Steps,
                starterCode = source.Challenge.StarterCode
            });
            node.Lesson.Solution = source.Solution;
            node.Lesson.ReferenceJson = source.Reference.ValueKind == JsonValueKind.Array ? source.Reference.GetRawText() : "[]";
            node.Lesson.Tests = source.Tests.Select((test, i) => new LessonTest
            {
                Key = test.Key,
                Label = test.Label,
                ExpectedOutput = test.ExpectedOutput,
                IsHidden = test.IsHidden,
                Order = i
            }).ToList();
        }

        // ---- problems ----

        public async Task<ResponseVM> GetProblemsAsync(string? kind, string? search, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var problems = _db.Problems.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(kind)) problems = problems.Where(x => x.Kind == kind);
            if (!string.IsNullOrWhiteSpace(search)) problems = problems.Where(x => x.Title.Contains(search) || x.Key.Contains(search));

            var total = await problems.CountAsync();
            var items = await problems.OrderBy(x => x.Kind).ThenBy(x => x.Number)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id, x.Key, x.Kind, x.Number, x.Title, x.Topic, x.Difficulty, x.Language, x.IsPublished, x.IsArchived,
                    tests = x.Tests.Count,
                    hiddenTests = x.Tests.Count(t => t.IsHidden)
                })
                .ToListAsync();
            return ResponseVM.Ok(new { items, total, page, pageSize });
        }

        public async Task<ResponseVM> GetProblemAsync(long problemId)
        {
            var problem = await _db.Problems.AsNoTracking().Include(x => x.Tests).FirstOrDefaultAsync(x => x.Id == problemId);
            if (problem == null)
            {
                return ResponseVM.NotFound("Problem not found.");
            }
            return ResponseVM.Ok(new
            {
                problem.Id, problem.Key, problem.Kind, problem.Number, problem.Title, problem.Topic, problem.Difficulty,
                problem.Language, problem.IsPublished, problem.IsArchived,
                detail = JsonDocument.Parse(problem.DetailJson).RootElement,
                tests = problem.Tests.OrderBy(t => t.Order).Select(t => new { t.Key, t.Args, t.Expected, t.IsHidden }),
                solvedBy = await _db.SolvedProblems.CountAsync(x => x.ProblemId == problemId)
            });
        }

        public async Task<ResponseVM> CreateProblemAsync(ProblemUpsertVM model)
        {
            var invalid = await ValidateProblemAsync(model, null);
            if (invalid != null)
            {
                return invalid;
            }

            var problem = new Problem { IsPublished = false };
            ApplyProblem(problem, model);
            _db.Problems.Add(problem);
            await _db.SaveChangesAsync();
            return ResponseVM.Ok(new { problem.Id }, "Problem created as a draft.");
        }

        public async Task<ResponseVM> UpdateProblemAsync(long problemId, ProblemUpsertVM model)
        {
            var problem = await _db.Problems.Include(x => x.Tests).FirstOrDefaultAsync(x => x.Id == problemId);
            if (problem == null)
            {
                return ResponseVM.NotFound("Problem not found.");
            }
            if (problem.Key != model.Key && await _db.ProblemSubmissions.AnyAsync(x => x.ProblemId == problemId))
            {
                return Conflict("key", "Learners have submitted to this problem, so its id cannot change.");
            }
            var invalid = await ValidateProblemAsync(model, problemId);
            if (invalid != null)
            {
                return invalid;
            }

            _db.ProblemTests.RemoveRange(problem.Tests);
            problem.Tests.Clear();
            ApplyProblem(problem, model);
            await _db.SaveChangesAsync();
            return ResponseVM.Ok(new { problem.Id }, "Problem saved.");
        }

        private async Task<ResponseVM?> ValidateProblemAsync(ProblemUpsertVM model, long? problemId)
        {
            if (await _db.Problems.AnyAsync(x => x.Key == model.Key && x.Id != problemId))
            {
                return Conflict("key", "A problem with this id already exists.");
            }
            if (model.Detail.ValueKind != JsonValueKind.Object)
            {
                return Invalid("detail", "Detail must be an object with the statement, hints, solution and starter code.");
            }
            if (model.Kind == "challenge" && (string.IsNullOrWhiteSpace(model.Language) || !await _db.Courses.AnyAsync(x => x.Slug == model.Language)))
            {
                return Invalid("language", "Challenges need the id of an existing course, e.g. \"python\".");
            }
            if (model.Tests.Select(t => t.Key).Distinct().Count() != model.Tests.Count)
            {
                return Invalid("tests", "Test ids must be unique.");
            }
            return null;
        }

        private static void ApplyProblem(Problem problem, ProblemUpsertVM model)
        {
            problem.Key = model.Key;
            problem.Kind = model.Kind;
            problem.Number = model.Number;
            problem.Title = model.Title.Trim();
            problem.Topic = model.Topic.Trim();
            problem.Difficulty = model.Difficulty;
            problem.Language = model.Kind == "challenge" ? model.Language : null;
            problem.DetailJson = model.Detail.GetRawText();
            problem.Tests = model.Tests.Select((test, i) => new ProblemTest
            {
                Key = test.Key,
                Args = test.Args,
                Expected = test.Expected,
                IsHidden = test.IsHidden,
                Order = i
            }).ToList();
        }

        // ---- cosmetics ----

        public async Task<ResponseVM> GetCosmeticsAsync() =>
            ResponseVM.Ok(await _db.CosmeticItems.AsNoTracking().OrderBy(x => x.Tab).ThenBy(x => x.Order).ToListAsync());

        public async Task<ResponseVM> CreateCosmeticAsync(CosmeticUpsertVM model)
        {
            if (await _db.CosmeticItems.AnyAsync(x => x.Key == model.Key))
            {
                return Conflict("key", "An item with this id already exists.");
            }
            var item = new CosmeticItem { IsPublished = false };
            ApplyCosmetic(item, model);
            _db.CosmeticItems.Add(item);
            await _db.SaveChangesAsync();
            return ResponseVM.Ok(new { item.Id }, "Item created as a draft.");
        }

        public async Task<ResponseVM> UpdateCosmeticAsync(long cosmeticId, CosmeticUpsertVM model)
        {
            var item = await _db.CosmeticItems.FirstOrDefaultAsync(x => x.Id == cosmeticId);
            if (item == null)
            {
                return ResponseVM.NotFound("Item not found.");
            }
            // Equipped items are referenced by key in learners' profiles.
            if (item.Key != model.Key)
            {
                return Conflict("key", "Item ids cannot change once created; create a new item instead.");
            }
            ApplyCosmetic(item, model);
            await _db.SaveChangesAsync();
            return ResponseVM.Ok(new { item.Id }, "Item saved.");
        }

        private static void ApplyCosmetic(CosmeticItem item, CosmeticUpsertVM model)
        {
            item.Key = model.Key;
            item.Kind = model.Kind;
            item.Tab = model.Kind switch { "theme" => "themes", "tutor" => "tutors", _ => "profile" };
            item.Name = model.Name.Trim();
            item.Description = model.Description.Trim();
            item.Art = model.Art;
            item.Rarity = model.Rarity;
            item.UnlockMetric = model.UnlockMetric;
            item.UnlockTarget = model.UnlockMetric == null ? 0 : model.UnlockTarget;
            item.Order = model.Order;
        }

        // ---- publish / archive ----

        public async Task<ResponseVM> SetStatusAsync(string type, long id, ContentStatusVM model)
        {
            IPublishable? content = type switch
            {
                "courses" => await _db.Courses.FindAsync(id),
                "sections" => await _db.CourseSections.FindAsync(id),
                "chapters" => await _db.Chapters.FindAsync(id),
                "nodes" => await _db.JourneyNodes.FindAsync(id),
                "problems" => await _db.Problems.FindAsync(id),
                "cosmetics" => await _db.CosmeticItems.FindAsync(id),
                _ => null
            };
            if (content == null)
            {
                return ResponseVM.NotFound("Content not found.");
            }

            if (model.IsPublished is { } published) content.IsPublished = published;
            if (model.IsArchived is { } archived) content.IsArchived = archived;
            await _db.SaveChangesAsync();
            return ResponseVM.Ok(new { id, isPublished = content.IsPublished, isArchived = content.IsArchived }, "Status saved.");
        }

        // ---- helpers ----

        private static bool SameSet(IEnumerable<long> existing, List<long> sent) =>
            sent.Distinct().Count() == sent.Count && existing.OrderBy(x => x).SequenceEqual(sent.OrderBy(x => x));

        private static ResponseVM Conflict(string field, string message)
        {
            var response = ResponseVM.Fail(409, ErrorCodes.Conflict, message);
            response.errors = new() { [field] = new[] { message } };
            return response;
        }

        private static ResponseVM Invalid(string field, string message)
        {
            var response = ResponseVM.BadRequest(ErrorCodes.Validation, message);
            response.errors = new() { [field] = new[] { message } };
            return response;
        }
    }
}
