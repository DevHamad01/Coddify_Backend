using Application.Interfaces.Learning;
using Application.VMs.Learning;
using Coddify_Backend.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coddify_Backend.Controllers.Learning
{
    /// <summary>Read a lesson, run code against its tests and complete it for rewards.</summary>
    [Authorize]
    [Route("api/lessons/{lessonId}")]
    public class LessonsController : BaseApiController
    {
        private readonly ILearningService _learning;

        public LessonsController(ILearningService learning)
        {
            _learning = learning;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string lessonId) =>
            Respond(await _learning.GetLessonAsync(UserId, lessonId));

        [EnableRateLimiting("run")]
        [HttpPost("run")]
        public async Task<IActionResult> Run(string lessonId, CodeVM model) =>
            Respond(await _learning.RunLessonAsync(UserId, lessonId, model));

        [EnableRateLimiting("run")]
        [HttpPost("complete")]
        public async Task<IActionResult> Complete(string lessonId, CompleteLessonVM model) =>
            Respond(await _learning.CompleteLessonAsync(UserId, lessonId, model));

        [HttpGet("submissions")]
        public async Task<IActionResult> Submissions(string lessonId) =>
            Respond(await _learning.GetLessonSubmissionsAsync(UserId, lessonId));
    }
}
