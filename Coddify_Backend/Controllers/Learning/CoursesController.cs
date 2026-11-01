using Application.Interfaces.Learning;
using Coddify_Backend.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coddify_Backend.Controllers.Learning
{
    /// <summary>Course catalog, learning paths, syllabus and the journey path.</summary>
    [Authorize]
    [Route("api")]
    public class CoursesController : BaseApiController
    {
        private readonly ILearningService _learning;

        public CoursesController(ILearningService learning)
        {
            _learning = learning;
        }

        [AllowAnonymous]
        [HttpGet("courses")]
        public async Task<IActionResult> Courses() => Respond(await _learning.GetCoursesAsync());

        [AllowAnonymous]
        [HttpGet("learning-paths")]
        public async Task<IActionResult> LearningPaths() => Respond(await _learning.GetLearningPathsAsync());

        [HttpGet("courses/{courseId}/syllabus")]
        public async Task<IActionResult> Syllabus(string courseId) =>
            Respond(await _learning.GetSyllabusAsync(UserId, courseId));

        [HttpGet("courses/{courseId}/journey")]
        public async Task<IActionResult> Journey(string courseId) =>
            Respond(await _learning.GetJourneyAsync(UserId, courseId));

        [HttpPost("journey/chests/{nodeId}/claim")]
        public async Task<IActionResult> ClaimChest(string nodeId) =>
            Respond(await _learning.ClaimChestAsync(UserId, nodeId));

        [HttpGet("me/courses")]
        public async Task<IActionResult> MyCourses() => Respond(await _learning.GetMyCoursesAsync(UserId));

        [HttpPost("me/courses/{courseId}/start")]
        public async Task<IActionResult> StartCourse(string courseId) =>
            Respond(await _learning.StartCourseAsync(UserId, courseId));
    }
}
