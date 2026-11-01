using Application.VMs;
using Application.VMs.Learning;

namespace Application.Interfaces.Learning
{
    /// <summary>Catalog, syllabus, journey path, lessons and enrollment for learners.</summary>
    public interface ILearningService
    {
        Task<ResponseVM> GetCoursesAsync();
        Task<ResponseVM> GetLearningPathsAsync();
        Task<ResponseVM> GetSyllabusAsync(long userId, string courseSlug);
        Task<ResponseVM> GetJourneyAsync(long userId, string courseSlug);
        Task<ResponseVM> GetMyCoursesAsync(long userId);
        Task<ResponseVM> StartCourseAsync(long userId, string courseSlug);
        Task<ResponseVM> ClaimChestAsync(long userId, string nodeKey);

        Task<ResponseVM> GetLessonAsync(long userId, string lessonKey);
        Task<ResponseVM> RunLessonAsync(long userId, string lessonKey, CodeVM model);
        Task<ResponseVM> CompleteLessonAsync(long userId, string lessonKey, CompleteLessonVM model);
        Task<ResponseVM> GetLessonSubmissionsAsync(long userId, string lessonKey);
    }
}
