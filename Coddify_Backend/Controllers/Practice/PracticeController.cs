using Application.Interfaces.Practice;
using Application.VMs.Practice;
using Coddify_Backend.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coddify_Backend.Controllers.Practice
{
    /// <summary>Interview prep, challenges and the problem workspace.</summary>
    [Authorize]
    [Route("api")]
    public class PracticeController : BaseApiController
    {
        private readonly IPracticeService _practice;

        public PracticeController(IPracticeService practice)
        {
            _practice = practice;
        }

        [HttpGet("interview/problems")]
        public async Task<IActionResult> Problems([FromQuery] ProblemQueryVM query) =>
            Respond(await _practice.GetProblemsAsync(UserId, query));

        [HttpGet("interview/problems/topics")]
        public async Task<IActionResult> Topics() => Respond(await _practice.GetTopicsAsync());

        [HttpGet("interview/today")]
        public async Task<IActionResult> Today() => Respond(await _practice.GetTodaySetAsync(UserId));

        [HttpGet("interview/progress")]
        public async Task<IActionResult> Progress() => Respond(await _practice.GetProgressAsync(UserId));

        [HttpPut("interview/date")]
        public async Task<IActionResult> InterviewDate(InterviewDateVM model) =>
            Respond(await _practice.SetInterviewDateAsync(UserId, model));

        [HttpGet("challenges")]
        public async Task<IActionResult> Challenges(
            [FromQuery(Name = "language")] string[] languages,
            [FromQuery(Name = "difficulty")] string[] difficulties,
            int page = 1,
            int pageSize = 20) =>
            Respond(await _practice.GetChallengesAsync(UserId, languages, difficulties, page, pageSize));

        [HttpGet("problems/{problemId}")]
        public async Task<IActionResult> Problem(string problemId) =>
            Respond(await _practice.GetProblemAsync(UserId, problemId));

        [EnableRateLimiting("run")]
        [HttpPost("problems/{problemId}/run")]
        public async Task<IActionResult> Run(string problemId, ProblemCodeVM model) =>
            Respond(await _practice.RunAsync(UserId, problemId, model));

        [EnableRateLimiting("run")]
        [HttpPost("problems/{problemId}/submit")]
        public async Task<IActionResult> Submit(string problemId, ProblemCodeVM model) =>
            Respond(await _practice.SubmitAsync(UserId, problemId, model));

        [HttpGet("problems/{problemId}/submissions")]
        public async Task<IActionResult> Submissions(string problemId) =>
            Respond(await _practice.GetSubmissionsAsync(UserId, problemId));
    }
}
