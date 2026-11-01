using Application.VMs;
using Application.VMs.Gamification;

namespace Application.Interfaces.Gamification
{
    public interface ILeaderboardService
    {
        Task<ResponseVM> GetLeaderboardAsync(long userId, string scope);
    }
}
