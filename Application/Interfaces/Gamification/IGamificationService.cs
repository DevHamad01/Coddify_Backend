using Application.VMs;
using Application.VMs.Gamification;

namespace Application.Interfaces.Gamification
{
    /// <summary>Stats, daily goal, streak and daily missions.</summary>
    public interface IGamificationService
    {
        Task<ResponseVM> GetStatsAsync(long userId);
        Task<ResponseVM> SetDailyGoalAsync(long userId, DailyGoalVM model);
        Task<ResponseVM> GetStreakAsync(long userId);
        Task<ResponseVM> GetMissionsAsync(long userId);
    }
}
