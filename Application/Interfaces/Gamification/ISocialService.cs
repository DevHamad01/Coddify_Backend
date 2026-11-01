using Application.VMs;
using Application.VMs.Gamification;

namespace Application.Interfaces.Gamification
{
    public interface ISocialService
    {
        Task<ResponseVM> FollowAsync(long userId, string username);
        Task<ResponseVM> UnfollowAsync(long userId, string username);
        Task<ResponseVM> SearchAsync(long userId, string query);
    }
}
