using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Application.VMs.Practice
{
    // Shapes match the frontend's practice.models.ts (camelCase JSON).

    public class PageVM<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class InterviewProblemVM
    {
        public string Id { get; set; } = "";
        public int Number { get; set; }
        public string Title { get; set; } = "";
        public string Topic { get; set; } = "";
        public string Difficulty { get; set; } = "";

        /// <summary>solved | in-progress | todo</summary>
        public string Status { get; set; } = "todo";

        /// <summary>Only set inside today's set.</summary>
        public bool? Surprise { get; set; }
    }

    public class ProblemQueryVM
    {
        public string? Search { get; set; }
        public string? Topic { get; set; }
        public string? Difficulty { get; set; }
        public string? Status { get; set; }

        /// <summary>number | title | topic | difficulty</summary>
        public string Sort { get; set; } = "number";

        /// <summary>asc | desc</summary>
        public string Direction { get; set; } = "asc";

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class InterviewSetVM
    {
        public string Title { get; set; } = "";
        public string Topic { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public List<InterviewProblemVM> Problems { get; set; } = new();
    }

    public class InterviewProgressVM
    {
        public Dictionary<string, int> Solved { get; set; } = new();
        public string? InterviewDate { get; set; }
    }

    public class InterviewDateVM
    {
        /// <summary>yyyy-MM-dd, or null to clear.</summary>
        public DateOnly? Date { get; set; }
    }

    public class ChallengeVM
    {
        public string Id { get; set; } = "";
        public int Number { get; set; }
        public string Title { get; set; } = "";
        public string Language { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string Status { get; set; } = "todo";
    }

    public class SimilarProblemVM
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string Topic { get; set; } = "";
    }

    public class ProblemTestVM
    {
        public string Id { get; set; } = "";
        public string Args { get; set; } = "";
        public string Expected { get; set; } = "";
    }

    /// <summary>
    /// Everything the problem workspace shows. The authored content (statement, hints, solution,
    /// starter code…) is passed through from DetailJson so admins can extend it without API changes.
    /// </summary>
    public class ProblemDetailVM
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public JsonElement Topics { get; set; }
        public JsonElement Statement { get; set; }
        public string FunctionName { get; set; } = "";
        public JsonElement Params { get; set; }
        public JsonElement Returns { get; set; }
        public JsonElement Constraints { get; set; }
        public JsonElement Examples { get; set; }
        public JsonElement Hints { get; set; }
        public JsonElement Solution { get; set; }
        public List<SimilarProblemVM> Similar { get; set; } = new();
        public JsonElement Languages { get; set; }
        public string DefaultLanguage { get; set; } = "";
        public JsonElement StarterCode { get; set; }
        public List<ProblemTestVM> Tests { get; set; } = new();
        public string Status { get; set; } = "todo";
    }

    public class ProblemCodeVM
    {
        [Required, RegularExpression("^(python|javascript|java|cpp)$")]
        public string Language { get; set; } = "python";

        [Required(AllowEmptyStrings = true), MaxLength(20000)]
        public string Code { get; set; } = "";
    }

    public class ProblemTestResultVM
    {
        public string TestId { get; set; } = "";
        public string Output { get; set; } = "";
        public bool Passed { get; set; }
    }

    public class ProblemRunResultVM
    {
        public List<ProblemTestResultVM> Results { get; set; } = new();
        public bool Passed { get; set; }
        public string? Error { get; set; }

        /// <summary>Hidden tests checked on submit (count only, never their contents).</summary>
        public int HiddenPassed { get; set; }
        public int HiddenTotal { get; set; }
    }

    public class ProblemSubmissionVM
    {
        public string Id { get; set; } = "";
        public DateTime SubmittedAt { get; set; }
        public string Language { get; set; } = "";
        public bool Passed { get; set; }
        public string Code { get; set; } = "";
    }
}
