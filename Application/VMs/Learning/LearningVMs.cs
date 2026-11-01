using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Application.VMs.Learning
{
    // Shapes match the frontend's learning.models.ts (camelCase JSON).

    public class JourneyNodeVM
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Kind { get; set; } = "";

        /// <summary>completed | current | locked</summary>
        public string State { get; set; } = "locked";

        public int Xp { get; set; }
    }

    public class JourneyChapterVM
    {
        public string Id { get; set; } = "";
        public int Number { get; set; }
        public string Title { get; set; } = "";
        public List<JourneyNodeVM> Nodes { get; set; } = new();
    }

    public class JourneyVM
    {
        public string CourseId { get; set; } = "";
        public string CourseName { get; set; } = "";
        public int SectionNumber { get; set; }
        public string SectionTitle { get; set; } = "";
        public List<JourneyChapterVM> Chapters { get; set; } = new();
    }

    public class TestCaseVM
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public string? ExpectedOutput { get; set; }
    }

    public class LessonChallengeVM
    {
        public string Level { get; set; } = "";
        public string Intro { get; set; } = "";
        public List<string> Steps { get; set; } = new();
        public string StarterCode { get; set; } = "";
        public string Solution { get; set; } = "";
        public List<TestCaseVM> Tests { get; set; } = new();
    }

    public class LessonVM
    {
        public string Id { get; set; } = "";
        public string CourseId { get; set; } = "";
        public string ChapterTitle { get; set; } = "";
        public string Title { get; set; } = "";
        public string EditorLanguage { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Summary { get; set; } = "";

        /// <summary>Lesson blocks: [{ type, text | code }].</summary>
        public JsonElement Content { get; set; }

        public LessonChallengeVM Challenge { get; set; } = new();
        public JsonElement Reference { get; set; }
        public int Xp { get; set; }
        public string? NextLessonId { get; set; }
    }

    public class CodeVM
    {
        [Required(AllowEmptyStrings = true), MaxLength(20000)]
        public string Code { get; set; } = "";
    }

    public class CompleteLessonVM : CodeVM
    {
        public bool FirstTry { get; set; }
    }

    public class TestResultVM
    {
        public string TestId { get; set; } = "";
        public bool Passed { get; set; }
    }

    public class RunResultVM
    {
        public string Output { get; set; } = "";
        public string? Error { get; set; }
        public List<TestResultVM> Tests { get; set; } = new();
        public bool Passed { get; set; }
    }

    public class LessonRewardVM
    {
        public int XpEarned { get; set; }
        public int GemsEarned { get; set; }
        public bool FirstTry { get; set; }
        public bool StreakIncreased { get; set; }
        public int Streak { get; set; }
    }

    public class SubmissionVM
    {
        public string Id { get; set; } = "";
        public DateTime SubmittedAt { get; set; }
        public bool Passed { get; set; }
        public string Code { get; set; } = "";
    }

    public class CourseSectionVM
    {
        public int Number { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int LessonCount { get; set; }
        public int CompletedCount { get; set; }

        /// <summary>in-progress | completed | available | locked</summary>
        public string State { get; set; } = "locked";
    }

    public class MiniCourseVM
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int LessonCount { get; set; }
        public bool Certificate { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class CourseSyllabusVM
    {
        public string CourseId { get; set; } = "";
        public string CourseName { get; set; } = "";
        public bool Enrolled { get; set; }
        public List<CourseSectionVM> Sections { get; set; } = new();
        public List<MiniCourseVM> MoreCourses { get; set; } = new();
    }

    public class EnrolledCourseVM
    {
        public string CourseId { get; set; } = "";
        public string CourseName { get; set; } = "";
        public int SectionNumber { get; set; }
        public int SectionCount { get; set; }
        public string SectionTitle { get; set; } = "";
        public int Percent { get; set; }
        public bool Active { get; set; }
    }

    public class CourseVM
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public List<string> Categories { get; set; } = new();
        public int Learners { get; set; }
        public bool InvertOnDark { get; set; }
    }

    public class LearningPathVM
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Summary { get; set; } = "";
        public List<string> Courses { get; set; } = new();
    }

    public class ChestRewardVM
    {
        public int Gems { get; set; }
    }
}
