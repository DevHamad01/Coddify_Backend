using Application.VMs;
using Application.VMs.Admin;

namespace Application.Interfaces.Admin
{
    /// <summary>
    /// Content authoring for admins. New content starts as a draft; nothing with learner progress is
    /// ever hard-deleted (archive instead) and public keys of used content cannot change, so the learner
    /// flow keeps working while admins edit.
    /// </summary>
    public interface IAdminContentService
    {
        Task<ResponseVM> GetDashboardAsync();

        // Courses and their tree
        Task<ResponseVM> GetCoursesAsync();
        Task<ResponseVM> GetCourseTreeAsync(long courseId);
        Task<ResponseVM> CreateCourseAsync(CourseUpsertVM model);
        Task<ResponseVM> UpdateCourseAsync(long courseId, CourseUpsertVM model);
        Task<ResponseVM> CreateSectionAsync(long courseId, SectionUpsertVM model);
        Task<ResponseVM> UpdateSectionAsync(long sectionId, SectionUpsertVM model);
        Task<ResponseVM> CreateChapterAsync(long sectionId, ChapterUpsertVM model);
        Task<ResponseVM> UpdateChapterAsync(long chapterId, ChapterUpsertVM model);
        Task<ResponseVM> ReorderChaptersAsync(long sectionId, ReorderVM model);
        Task<ResponseVM> GetNodeAsync(long nodeId);
        Task<ResponseVM> CreateNodeAsync(long chapterId, NodeUpsertVM model);
        Task<ResponseVM> UpdateNodeAsync(long nodeId, NodeUpsertVM model);
        Task<ResponseVM> ReorderNodesAsync(long chapterId, ReorderVM model);

        // Practice
        Task<ResponseVM> GetProblemsAsync(string? kind, string? search, int page, int pageSize);
        Task<ResponseVM> GetProblemAsync(long problemId);
        Task<ResponseVM> CreateProblemAsync(ProblemUpsertVM model);
        Task<ResponseVM> UpdateProblemAsync(long problemId, ProblemUpsertVM model);

        // Store
        Task<ResponseVM> GetCosmeticsAsync();
        Task<ResponseVM> CreateCosmeticAsync(CosmeticUpsertVM model);
        Task<ResponseVM> UpdateCosmeticAsync(long cosmeticId, CosmeticUpsertVM model);

        /// <param name="type">courses | sections | chapters | nodes | problems | cosmetics</param>
        Task<ResponseVM> SetStatusAsync(string type, long id, ContentStatusVM model);
    }
}
