using Application.Interfaces.Admin;
using Application.VMs.Admin;
using Coddify_Backend.Controllers.Common;
using CommonOperations.Constants.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coddify_Backend.Controllers.Admin
{
    /// <summary>
    /// Content authoring for the admin panel: courses (sections, chapters, nodes, lessons and tests),
    /// practice problems and store cosmetics. New content is a draft until published; use the status
    /// endpoint to publish or archive (there is no hard delete, so learner progress is never broken).
    /// </summary>
    [Authorize(Roles = Roles.Admin)]
    [Route("api/admin")]
    public class AdminContentController : BaseApiController
    {
        private readonly IAdminContentService _content;

        public AdminContentController(IAdminContentService content)
        {
            _content = content;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard() => Respond(await _content.GetDashboardAsync());

        // Courses
        [HttpGet("courses")]
        public async Task<IActionResult> Courses() => Respond(await _content.GetCoursesAsync());

        [HttpGet("courses/{id:long}")]
        public async Task<IActionResult> CourseTree(long id) => Respond(await _content.GetCourseTreeAsync(id));

        [HttpPost("courses")]
        public async Task<IActionResult> CreateCourse(CourseUpsertVM model) => Respond(await _content.CreateCourseAsync(model));

        [HttpPut("courses/{id:long}")]
        public async Task<IActionResult> UpdateCourse(long id, CourseUpsertVM model) => Respond(await _content.UpdateCourseAsync(id, model));

        // Sections
        [HttpPost("courses/{courseId:long}/sections")]
        public async Task<IActionResult> CreateSection(long courseId, SectionUpsertVM model) =>
            Respond(await _content.CreateSectionAsync(courseId, model));

        [HttpPut("sections/{id:long}")]
        public async Task<IActionResult> UpdateSection(long id, SectionUpsertVM model) => Respond(await _content.UpdateSectionAsync(id, model));

        [HttpPut("sections/{id:long}/chapters/order")]
        public async Task<IActionResult> ReorderChapters(long id, ReorderVM model) => Respond(await _content.ReorderChaptersAsync(id, model));

        // Chapters
        [HttpPost("sections/{sectionId:long}/chapters")]
        public async Task<IActionResult> CreateChapter(long sectionId, ChapterUpsertVM model) =>
            Respond(await _content.CreateChapterAsync(sectionId, model));

        [HttpPut("chapters/{id:long}")]
        public async Task<IActionResult> UpdateChapter(long id, ChapterUpsertVM model) => Respond(await _content.UpdateChapterAsync(id, model));

        [HttpPut("chapters/{id:long}/nodes/order")]
        public async Task<IActionResult> ReorderNodes(long id, ReorderVM model) => Respond(await _content.ReorderNodesAsync(id, model));

        // Nodes (lessons, quizzes, practice, chests)
        [HttpPost("chapters/{chapterId:long}/nodes")]
        public async Task<IActionResult> CreateNode(long chapterId, NodeUpsertVM model) =>
            Respond(await _content.CreateNodeAsync(chapterId, model));

        [HttpGet("nodes/{id:long}")]
        public async Task<IActionResult> Node(long id) => Respond(await _content.GetNodeAsync(id));

        [HttpPut("nodes/{id:long}")]
        public async Task<IActionResult> UpdateNode(long id, NodeUpsertVM model) => Respond(await _content.UpdateNodeAsync(id, model));

        // Practice problems and challenges
        [HttpGet("problems")]
        public async Task<IActionResult> Problems(string? kind, string? search, int page = 1, int pageSize = 20) =>
            Respond(await _content.GetProblemsAsync(kind, search, page, pageSize));

        [HttpGet("problems/{id:long}")]
        public async Task<IActionResult> Problem(long id) => Respond(await _content.GetProblemAsync(id));

        [HttpPost("problems")]
        public async Task<IActionResult> CreateProblem(ProblemUpsertVM model) => Respond(await _content.CreateProblemAsync(model));

        [HttpPut("problems/{id:long}")]
        public async Task<IActionResult> UpdateProblem(long id, ProblemUpsertVM model) => Respond(await _content.UpdateProblemAsync(id, model));

        // Store cosmetics
        [HttpGet("cosmetics")]
        public async Task<IActionResult> Cosmetics() => Respond(await _content.GetCosmeticsAsync());

        [HttpPost("cosmetics")]
        public async Task<IActionResult> CreateCosmetic(CosmeticUpsertVM model) => Respond(await _content.CreateCosmeticAsync(model));

        [HttpPut("cosmetics/{id:long}")]
        public async Task<IActionResult> UpdateCosmetic(long id, CosmeticUpsertVM model) => Respond(await _content.UpdateCosmeticAsync(id, model));

        /// <summary>Publish/unpublish or archive/restore: type = courses | sections | chapters | nodes | problems | cosmetics.</summary>
        [HttpPatch("{type}/{id:long}/status")]
        public async Task<IActionResult> Status(string type, long id, ContentStatusVM model) =>
            Respond(await _content.SetStatusAsync(type, id, model));
    }
}
