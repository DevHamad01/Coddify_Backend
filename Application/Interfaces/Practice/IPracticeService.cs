using Application.VMs;
using Application.VMs.Practice;

namespace Application.Interfaces.Practice
{
    /// <summary>Interview prep, the challenge bank and the problem workspace.</summary>
    public interface IPracticeService
    {
        Task<ResponseVM> GetProblemsAsync(long userId, ProblemQueryVM query);
        Task<ResponseVM> GetTopicsAsync();
        Task<ResponseVM> GetTodaySetAsync(long userId);
        Task<ResponseVM> GetProgressAsync(long userId);
        Task<ResponseVM> SetInterviewDateAsync(long userId, InterviewDateVM model);
        Task<ResponseVM> GetChallengesAsync(long userId, string[] languages, string[] difficulties, int page, int pageSize);

        Task<ResponseVM> GetProblemAsync(long userId, string problemKey);
        Task<ResponseVM> RunAsync(long userId, string problemKey, ProblemCodeVM model);
        Task<ResponseVM> SubmitAsync(long userId, string problemKey, ProblemCodeVM model);
        Task<ResponseVM> GetSubmissionsAsync(long userId, string problemKey);
    }
}
