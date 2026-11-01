using Application.Interfaces.AppUsers;
using Application.VMs.AppUsers.Authentication;
using Coddify_Backend.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coddify_Backend.Controllers.AppUsers
{
    /// <summary>Sign-up, sign-in, email verification and passwords. Routes match the frontend's api-endpoints.ts.</summary>
    [Route("api/auth")]
    [EnableRateLimiting("auth")]
    public class AuthController : BaseApiController
    {
        private readonly IUserAuthentication _services;

        public AuthController(IUserAuthentication services)
        {
            _services = services;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterVM model) =>
            Respond(await _services.RegisterAsync(model));

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginVM model) =>
            Respond(await _services.LoginAsync(model));

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginVM model) =>
            Respond(await _services.LoginWithGoogleAsync(model));

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailVM model) =>
            Respond(await _services.VerifyEmailAsync(model));

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification(EmailVM model) =>
            Respond(await _services.ResendVerificationAsync(model));

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(EmailVM model) =>
            Respond(await _services.ForgotPasswordAsync(model));

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM model) =>
            Respond(await _services.ResetPasswordAsync(model));

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenVM model) =>
            Respond(await _services.RefreshTokenAsync(model));

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM model) =>
            Respond(await _services.ChangePasswordAsync(UserId, model));

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout() =>
            Respond(await _services.LogoutAsync(UserId));
    }
}
