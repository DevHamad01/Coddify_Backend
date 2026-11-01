using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domian.Entities.Learning
{
    /// <summary>A language/technology course ("python"). Admins author it; learners read published ones.</summary>
    public class Course
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>Stable public id used in URLs and by the frontend, e.g. "python".</summary>
        [MaxLength(50)]
        public string Slug { get; set; } = "";

        [MaxLength(100)]
        public string Name { get; set; } = "";

        /// <summary>Comma separated catalog categories: popular,web,languages…</summary>
        [MaxLength(200)]
        public string Categories { get; set; } = "";

        public int LearnerCount { get; set; }

        /// <summary>Logo is mostly black and needs flipping on the dark theme.</summary>
        public bool InvertOnDark { get; set; }

        public int Order { get; set; }
        public bool IsPublished { get; set; } = true;
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public List<CourseSection> Sections { get; set; } = new();
        public List<MiniCourse> MiniCourses { get; set; } = new();
    }

    /// <summary>"Section 1 · Fundamentals". Holds the chapters of the journey path.</summary>
    public class CourseSection
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long CourseId { get; set; }
        public Course? Course { get; set; }

        /// <summary>1-based position inside the course.</summary>
        public int Number { get; set; }

        [MaxLength(150)]
        public string Title { get; set; } = "";

        [MaxLength(500)]
        public string Description { get; set; } = "";

        /// <summary>Planned lesson total shown on the syllabus ("1/77 lessons").</summary>
        public int LessonCount { get; set; }

        public bool IsPublished { get; set; } = true;
        public bool IsArchived { get; set; }

        public List<Chapter> Chapters { get; set; } = new();
    }

    public class Chapter
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long SectionId { get; set; }
        public CourseSection? Section { get; set; }

        /// <summary>Stable public id, e.g. "introduction".</summary>
        [MaxLength(100)]
        public string Key { get; set; } = "";

        [MaxLength(150)]
        public string Title { get; set; } = "";

        public int Order { get; set; }
        public bool IsPublished { get; set; } = true;
        public bool IsArchived { get; set; }

        public List<JourneyNode> Nodes { get; set; } = new();
    }

    /// <summary>One hexagon on the path: lesson, quiz, practice or chapter chest.</summary>
    public class JourneyNode
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long ChapterId { get; set; }
        public Chapter? Chapter { get; set; }

        /// <summary>Stable public id, e.g. "py-the-language". Learner progress points at it, so it never changes.</summary>
        [MaxLength(100)]
        public string Key { get; set; } = "";

        [MaxLength(150)]
        public string Title { get; set; } = "";

        /// <summary>lesson | quiz | practice | chest</summary>
        [MaxLength(20)]
        public string Kind { get; set; } = "lesson";

        public int Xp { get; set; }
        public int Order { get; set; }
        public bool IsPublished { get; set; } = true;
        public bool IsArchived { get; set; }

        public Lesson? Lesson { get; set; }
    }

    /// <summary>Content and challenge of a lesson/quiz/practice node (chests have none).</summary>
    public class Lesson
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long NodeId { get; set; }
        public JourneyNode? Node { get; set; }

        [MaxLength(500)]
        public string Summary { get; set; } = "";

        /// <summary>Monaco language id, e.g. "python".</summary>
        [MaxLength(30)]
        public string EditorLanguage { get; set; } = "python";

        [MaxLength(50)]
        public string FileName { get; set; } = "main.py";

        /// <summary>Lesson blocks as JSON: [{ type: "paragraph" | "code" | "tip", text/code }].</summary>
        public string ContentJson { get; set; } = "[]";

        /// <summary>Challenge as JSON: { level, intro, steps[], starterCode }.</summary>
        public string ChallengeJson { get; set; } = "{}";

        /// <summary>Reference cards as JSON: [{ title, body, code? }].</summary>
        public string ReferenceJson { get; set; } = "[]";

        /// <summary>Model solution. Never sent to learners.</summary>
        public string Solution { get; set; } = "";

        public List<LessonTest> Tests { get; set; } = new();
    }

    public class LessonTest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long LessonId { get; set; }
        public Lesson? Lesson { get; set; }

        /// <summary>Public id inside the lesson, e.g. "t1".</summary>
        [MaxLength(20)]
        public string Key { get; set; } = "";

        [MaxLength(200)]
        public string Label { get; set; } = "";

        /// <summary>null: any output passes as long as the code runs without errors.</summary>
        public string? ExpectedOutput { get; set; }

        /// <summary>Hidden tests are checked on completion but never shown to the learner.</summary>
        public bool IsHidden { get; set; }

        public int Order { get; set; }
    }

    /// <summary>Short standalone course listed under "More courses in Python".</summary>
    public class MiniCourse
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long CourseId { get; set; }
        public Course? Course { get; set; }

        [MaxLength(100)]
        public string Key { get; set; } = "";

        [MaxLength(200)]
        public string Title { get; set; } = "";

        [MaxLength(500)]
        public string Description { get; set; } = "";

        public int LessonCount { get; set; }
        public bool Certificate { get; set; }

        /// <summary>Comma separated tags.</summary>
        [MaxLength(300)]
        public string Tags { get; set; } = "";

        public int Order { get; set; }
        public bool IsPublished { get; set; } = true;
        public bool IsArchived { get; set; }
    }

    /// <summary>Career path: several courses in the order a job needs them.</summary>
    public class LearningPath
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [MaxLength(50)]
        public string Slug { get; set; } = "";

        [MaxLength(150)]
        public string Title { get; set; } = "";

        [MaxLength(500)]
        public string Summary { get; set; } = "";

        public int Order { get; set; }
        public bool IsPublished { get; set; } = true;
        public bool IsArchived { get; set; }

        public List<LearningPathCourse> Courses { get; set; } = new();
    }

    public class LearningPathCourse
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long PathId { get; set; }
        public LearningPath? Path { get; set; }

        public long CourseId { get; set; }
        public Course? Course { get; set; }

        public int Order { get; set; }
    }
}
