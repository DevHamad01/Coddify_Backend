using System.ComponentModel.DataAnnotations;

namespace Application.VMs.Tutor
{
    /// <summary>Last code run the learner did, as the frontend sends it (RunResult).</summary>
    public class TutorRunVM
    {
        public string Output { get; set; } = "";
        public string? Error { get; set; }
        public bool Passed { get; set; }
    }

    /// <summary>Body of POST /api/tutor/chat (frontend tutor-api.ts TutorRequest).</summary>
    public class TutorRequestVM
    {
        /// <summary>Lesson key; leave empty when asking about a practice problem.</summary>
        [MaxLength(100)]
        public string? LessonId { get; set; }

        /// <summary>Practice problem key ("p-1", "c-python-3"); used when LessonId is empty.</summary>
        [MaxLength(100)]
        public string? ProblemId { get; set; }

        [Required, StringLength(1000, MinimumLength = 1)]
        public string Message { get; set; } = "";

        [MaxLength(20000)]
        public string Code { get; set; } = "";

        public TutorRunVM? LastRun { get; set; }
    }

    public class TutorReplyVM
    {
        public string Text { get; set; } = "";
    }
}
