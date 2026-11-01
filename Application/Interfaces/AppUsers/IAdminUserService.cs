using Application.VMs;
using Application.VMs.AppUsers.Authentication;

namespace Application.Interfaces.AppUsers
{
    /// <summary>Admin actions on learner accounts.</summary>
    public interface IAdminUserService
    {
        Task<ResponseVM> SearchAsync(string? query, int page, int pageSize);
        Task<ResponseVM> BlockAsync(DeclineUserVM model);
        Task<ResponseVM> UnblockAsync(long id);
        Task<ResponseVM> DeleteAsync(long id);
    }
}
