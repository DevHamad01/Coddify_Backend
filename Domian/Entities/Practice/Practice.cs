using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domian.Entities.Common;

namespace Domian.Entities.Practice
{
    /// <summary>An interview problem or a language challenge, solved in the problem workspace.</summary>
    public class Problem : IPublishable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>Stable public id: "p-1" (interview) or "c-python-1" (challenge).</summary>
        [MaxLength(60)]
        public string Key { get; set; } = "";

        /// <summary>interview | challenge</summary>
        [MaxLength(20)]
        public string Kind { get; set; } = "interview";

        public int Number { get; set; }

        [MaxLength(200)]
        public string Title { get; set; } = "";

        [MaxLength(100)]
        public string Topic { get; set; } = "";

        /// <summary>Beginner | Easy | Medium | Hard</summary>
        [MaxLength(20)]
        public string Difficulty { get; set; } = "Easy";

        /// <summary>Course slug a challenge is written for; null for interview problems.</summary>
        [MaxLength(50)]
        public string? Language { get; set; }

        /// <summary>
        /// Workspace content as JSON: statement, functionName, params, returns, constraints, examples,
        /// hints, solution, languages, defaultLanguage, starterCode.
        /// </summary>
        public string DetailJson { get; set; } = "{}";

        public bool IsPublished { get; set; } = true;
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<ProblemTest> Tests { get; set; } = new();
    }

    public class ProblemTest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long ProblemId { get; set; }
        public Problem? Problem { get; set; }

        [MaxLength(20)]
        public string Key { get; set; } = "";

        /// <summary>Arguments as shown to the learner, e.g. "\"Coddy2026\"".</summary>
        public string Args { get; set; } = "";

        public string Expected { get; set; } = "";

        /// <summary>Hidden tests run on submit only and are never sent to the learner.</summary>
        public bool IsHidden { get; set; }

        public int Order { get; set; }
    }

    public class ProblemSubmission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long UserId { get; set; }
        public long ProblemId { get; set; }

        [MaxLength(30)]
        public string Language { get; set; } = "";

        public string Code { get; set; } = "";
        public bool Passed { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }

    public class SolvedProblem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long UserId { get; set; }
        public long ProblemId { get; set; }
        public DateTime SolvedAt { get; set; } = DateTime.UtcNow;
    }
}
