using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Application.VMs.Admin
{
    /// <summary>Publish or archive any content item. Archived content disappears for learners but keeps their progress.</summary>
    public class ContentStatusVM
    {
        public bool? IsPublished { get; set; }
        public bool? IsArchived { get; set; }
    }

    /// <summary>New order of child items, by id (first = shown first).</summary>
    public class ReorderVM
    {
        [Required, MinLength(1)]
        public List<long> Ids { get; set; } = new();
    }

    public class CourseUpsertVM
    {
        [Required, RegularExpression("^[a-z0-9-]{2,50}$", ErrorMessage = "Use 2-50 lowercase letters, numbers or dashes.")]
        public string Slug { get; set; } = "";

        [Required, StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = "";

        public List<string> Categories { get; set; } = new();
        public int LearnerCount { get; set; }
        public bool InvertOnDark { get; set; }
        public int Order { get; set; }
    }

    public class SectionUpsertVM
    {
        [Range(1, 100)]
        public int Number { get; set; }

        [Required, StringLength(150, MinimumLength = 1)]
        public string Title { get; set; } = "";

        [MaxLength(500)]
        public string Description { get; set; } = "";

        [Range(0, 1000)]
        public int LessonCount { get; set; }
    }

    public class ChapterUpsertVM
    {
        [Required, RegularExpression("^[a-z0-9~-]{2,100}$")]
        public string Key { get; set; } = "";

        [Required, StringLength(150, MinimumLength = 1)]
        public string Title { get; set; } = "";

        public int Order { get; set; }
    }

    public class LessonTestUpsertVM
    {
        [Required, MaxLength(20)]
        public string Key { get; set; } = "";

        [Required, MaxLength(200)]
        public string Label { get; set; } = "";

        /// <summary>null: any output passes as long as the code runs.</summary>
        public string? ExpectedOutput { get; set; }

        public bool IsHidden { get; set; }
    }

    public class LessonChallengeUpsertVM
    {
        [RegularExpression("^(Beginner|Easy|Medium|Hard)$")]
        public string Level { get; set; } = "Beginner";

        [MaxLength(1000)]
        public string Intro { get; set; } = "";

        public List<string> Steps { get; set; } = new();

        [MaxLength(20000)]
        public string StarterCode { get; set; } = "";
    }

    public class LessonUpsertVM
    {
        [MaxLength(500)]
        public string Summary { get; set; } = "";

        [Required, MaxLength(30)]
        public string EditorLanguage { get; set; } = "python";

        [Required, MaxLength(50)]
        public string FileName { get; set; } = "main.py";

        /// <summary>Lesson blocks: [{ "type": "paragraph" | "code" | "tip", "text" | "code": "…" }].</summary>
        public JsonElement Content { get; set; }

        public LessonChallengeUpsertVM Challenge { get; set; } = new();

        [MaxLength(20000)]
        public string Solution { get; set; } = "";

        /// <summary>Reference cards: [{ "title", "body", "code"? }].</summary>
        public JsonElement Reference { get; set; }

        public List<LessonTestUpsertVM> Tests { get; set; } = new();
    }

    public class NodeUpsertVM
    {
        [Required, RegularExpression("^[a-z0-9~-]{2,100}$")]
        public string Key { get; set; } = "";

        [Required, StringLength(150, MinimumLength = 1)]
        public string Title { get; set; } = "";

        [Required, RegularExpression("^(lesson|quiz|practice|chest)$")]
        public string Kind { get; set; } = "lesson";

        [Range(0, 1000)]
        public int Xp { get; set; }

        public int Order { get; set; }

        /// <summary>Required for lesson, quiz and practice nodes; ignored for chests.</summary>
        public LessonUpsertVM? Lesson { get; set; }
    }

    public class ProblemTestUpsertVM
    {
        [Required, MaxLength(20)]
        public string Key { get; set; } = "";

        [Required]
        public string Args { get; set; } = "";

        [Required(AllowEmptyStrings = true)]
        public string Expected { get; set; } = "";

        public bool IsHidden { get; set; }
    }

    public class ProblemUpsertVM
    {
        [Required, RegularExpression("^[a-z0-9-]{2,60}$")]
        public string Key { get; set; } = "";

        [Required, RegularExpression("^(interview|challenge)$")]
        public string Kind { get; set; } = "interview";

        [Range(1, 100000)]
        public int Number { get; set; }

        [Required, StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = "";

        [Required, MaxLength(100)]
        public string Topic { get; set; } = "";

        [Required, RegularExpression("^(Beginner|Easy|Medium|Hard)$")]
        public string Difficulty { get; set; } = "Easy";

        /// <summary>Course slug for challenges; null for interview problems.</summary>
        [MaxLength(50)]
        public string? Language { get; set; }

        /// <summary>
        /// Workspace content object: statement[], functionName, params[], returns, constraints[], examples[],
        /// hints[], solution, languages[], defaultLanguage, starterCode{}.
        /// </summary>
        public JsonElement Detail { get; set; }

        [MinLength(1)]
        public List<ProblemTestUpsertVM> Tests { get; set; } = new();
    }

    public class CosmeticUpsertVM
    {
        [Required, RegularExpression("^[a-z0-9-]{2,100}$")]
        public string Key { get; set; } = "";

        [Required, RegularExpression("^(title|frame|name-effect|streak-flame|theme|tutor)$")]
        public string Kind { get; set; } = "title";

        [Required, StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = "";

        [MaxLength(300)]
        public string Description { get; set; } = "";

        [Required, MaxLength(50)]
        public string Art { get; set; } = "";

        [RegularExpression("^(common|rare|legendary)$")]
        public string Rarity { get; set; } = "common";

        /// <summary>xp | level | streak | lessons | solved; null = unlocked from the start.</summary>
        [RegularExpression("^(xp|level|streak|lessons|solved)$")]
        public string? UnlockMetric { get; set; }

        [Range(0, 1000000)]
        public int UnlockTarget { get; set; }

        public int Order { get; set; }
    }
}
