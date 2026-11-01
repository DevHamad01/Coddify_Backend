using Application.VMs;
using Application.VMs.AppUsers.Authentication;

namespace Application.Interfaces.AppUsers
{
    /// <summary>Sign-up, sign-in, email verification, passwords and sessions.</summary>
    public interface IUserAuthentication
    {
        Task<ResponseVM> RegisterAsync(RegisterVM model);
        Task<ResponseVM> LoginAsync(LoginVM model);
        Task<ResponseVM> LoginWithGoogleAsync(GoogleLoginVM model);
        Task<ResponseVM> VerifyEmailAsync(VerifyEmailVM model);
        Task<ResponseVM> ResendVerificationAsync(EmailVM model);
        Task<ResponseVM> ForgotPasswordAsync(EmailVM model);
        Task<ResponseVM> ResetPasswordAsync(ResetPasswordVM model);
        Task<ResponseVM> ChangePasswordAsync(long userId, ChangePasswordVM model);
        Task<ResponseVM> RefreshTokenAsync(RefreshTokenVM model);
        Task<ResponseVM> LogoutAsync(long userId);
        Task<ResponseVM> GetCurrentUserAsync(long userId);
    }
}
