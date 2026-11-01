using Application.VMs;
using Application.VMs.Gamification;

namespace Application.Interfaces.Gamification
{
    public interface IProfileService
    {
        Task<ResponseVM> GetProfileAsync(long userId);
        Task<ResponseVM> UpdateProfileAsync(long userId, ProfileUpdateVM model);
        Task<ResponseVM> SetShowcaseAsync(long userId, ShowcaseVM model);
        Task<ResponseVM> SetAvatarAsync(long userId, AvatarLookVM model);
        Task<ResponseVM> GetCollectionAsync(long userId);
        Task<ResponseVM> GetLevelsAsync(long userId);
        Task<ResponseVM> GetPublicProfileAsync(long viewerId, string username);
    }
}
