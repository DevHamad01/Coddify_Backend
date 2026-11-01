using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domian.Entities.Progress
{
    /// <summary>A course the learner has started. Exactly one enrollment per user is active.</summary>
    public class Enrollment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long UserId { get; set; }
        public long CourseId { get; set; }
        public bool IsActive { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastOpenedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>A finished journey node (lesson, quiz, practice) or an opened chapter chest.</summary>
    public class NodeCompletion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long UserId { get; set; }
        public long NodeId { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }

    public class LessonSubmission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long UserId { get; set; }
        public long NodeId { get; set; }
        public string Code { get; set; } = "";
        public bool Passed { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
