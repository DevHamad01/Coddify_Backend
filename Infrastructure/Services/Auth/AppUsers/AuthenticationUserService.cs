using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Application.Interfaces.AppUsers;
using Application.VMs;
using Application.VMs.AppUsers.Authentication;
using CommonOperations.Constants.Common;
using CommonOperations.Methods;
using CommonOperations.Security;
using Domian.Entities.AppUsers;
using Google.Apis.Auth;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Auth.AppUsers
{
    /// <summary>
    /// Sign-up and sign-in flow (first written by Isra), hardened: email-link tokens are signed and
    /// validated, passwords use salted PBKDF2, refresh tokens are stored hashed and rotate on use.
    /// </summary>
    public class AuthenticationUserService : IUserAuthentication
    {
        private const string VerifyPurpose = "verify";
        private const string ResetPurpose = "reset";
        private static readonly TimeSpan VerifyLifetime = TimeSpan.FromHours(24);
        private static readonly TimeSpan ResetLifetime = TimeSpan.FromHours(1);
        private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);

        private readonly IConfiguration _config;
        private readonly AppDbContext _context;
        private readonly ILogger<AuthenticationUserService> _logger;

        public AuthenticationUserService(
            IConfiguration config,
            AppDbContext context,
            ILogger<AuthenticationUserService> logger)
        {
            _config = config;
            _context = context;
            _logger = logger;
        }

        public async Task<ResponseVM> RegisterAsync(RegisterVM model)
        {
            var email = NormaliseEmail(model.Email);
            if (await _context.AppUsers.AnyAsync(x => x.Email == email && !x.IsDeleted))
            {
                return ResponseVM.Fail(409, ErrorCodes.EmailTaken, "An account with this email already exists.");
            }

            var user = new AppUser
            {
                Email = email,
                UserName = await UniqueUsernameAsync(model.FullName, email),
                Password = PasswordHasher.Hash(model.Password),
                FullName = model.FullName.Trim(),
                Role = Roles.Learner,
                CreatedAt = DateTime.UtcNow
            };

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();
            await SendVerifyEmailAsync(user);

            return ResponseVM.Ok(
                new RegisterResultVM { Email = user.Email, RequiresVerification = true },
                "Account created. Check your inbox to verify your email.");
        }

        public async Task<ResponseVM> LoginAsync(LoginVM model)
        {
            var email = NormaliseEmail(model.Email);
            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted);

            // Same message for "no account" and "wrong password" so emails cannot be probed.
            if (user == null || !PasswordHasher.Verify(model.Password, user.Password, out var needsRehash))
            {
                return ResponseVM.Fail(401, ErrorCodes.InvalidCredentials, "Wrong email or password.");
            }

            if (user.IsBlocked)
            {
                return ResponseVM.Fail(403, ErrorCodes.Blocked, "This account is blocked.");
            }

            if (!user.IsEmailVerified)
            {
                return ResponseVM.Fail(403, ErrorCodes.EmailNotVerified, "Please verify your email before logging in.");
            }

            if (needsRehash)
            {
                user.Password = PasswordHasher.Hash(model.Password);
            }

            return ResponseVM.Ok(await StartSessionAsync(user), "Login successful.");
        }

        public async Task<ResponseVM> LoginWithGoogleAsync(GoogleLoginVM model)
        {
            var clientId = _config["Google:ClientId"] ?? _config["Google:Client_id"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return ResponseVM.Fail(501, ErrorCodes.GoogleNotConfigured, "Google sign-in is not configured.");
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(
                    model.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } });
            }
            catch (InvalidJwtException)
            {
                return ResponseVM.Fail(401, ErrorCodes.InvalidToken, "Google sign-in failed. Please try again.");
            }

            var email = NormaliseEmail(payload.Email);
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

            if (user is { IsBlocked: true })
            {
                return ResponseVM.Fail(403, ErrorCodes.Blocked, "This account is blocked.");
            }

            if (user == null)
            {
                user = new AppUser
                {
                    FullName = payload.Name ?? email.Split('@')[0],
                    Email = email,
                    Password = "",
                    ProfileImageUrl = payload.Picture ?? "",
                    IsEmailVerified = true,
                    UserName = await UniqueUsernameAsync(payload.Name ?? "", email),
                    Role = Roles.Learner,
                    CreatedAt = DateTime.UtcNow
                };
                _context.AppUsers.Add(user);
            }
            else if (!user.IsEmailVerified)
            {
                // Google has already proven the address belongs to this person.
                user.IsEmailVerified = true;
            }

            return ResponseVM.Ok(await StartSessionAsync(user), "Login successful.");
        }

        public async Task<ResponseVM> VerifyEmailAsync(VerifyEmailVM model)
        {
            var user = await FindByTokenAsync(model.Token, model.Email, VerifyPurpose);
            if (user.Error != null)
            {
                return user.Error;
            }

            if (!user.User!.IsEmailVerified)
            {
                user.User.IsEmailVerified = true;
                user.User.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return ResponseVM.Ok(new MessageVM("Email verified. You can log in now."), "Email verified.");
        }

        public async Task<ResponseVM> ResendVerificationAsync(EmailVM model)
        {
            var email = NormaliseEmail(model.Email);
            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted);

            // Answer the same way whether or not the account exists.
            if (user != null && !user.IsEmailVerified && !user.IsBlocked)
            {
                await SendVerifyEmailAsync(user);
            }

            return ResponseVM.Ok(
                new RegisterResultVM { Email = email, RequiresVerification = true },
                "If that account needs verifying, a new email is on its way.");
        }

        public async Task<ResponseVM> ForgotPasswordAsync(EmailVM model)
        {
            var email = NormaliseEmail(model.Email);
            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted);

            if (user != null && !user.IsBlocked)
            {
                await SendResetEmailAsync(user);
            }

            return ResponseVM.Ok(
                new MessageVM("If an account exists for that email, a reset link is on its way."),
                "Reset email sent.");
        }

        public async Task<ResponseVM> ResetPasswordAsync(ResetPasswordVM model)
        {
            var found = await FindByTokenAsync(model.Token, model.Email, ResetPurpose);
            if (found.Error != null)
            {
                return found.Error;
            }

            var user = found.User!;
            user.Password = PasswordHasher.Hash(model.NewPassword);
            // Signing in again everywhere is safer after a reset.
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ResponseVM.Ok(new MessageVM("Password updated. Log in with your new password."), "Password updated.");
        }

        public async Task<ResponseVM> ChangePasswordAsync(long userId, ChangePasswordVM model)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);
            if (user == null)
            {
                return ResponseVM.NotFound("User not found.", ErrorCodes.UserNotFound);
            }

            // Google accounts may not have a password yet; everyone else must prove the old one.
            if (!string.IsNullOrEmpty(user.Password) && !PasswordHasher.Verify(model.OldPassword, user.Password, out _))
            {
                return ResponseVM.BadRequest(ErrorCodes.InvalidCredentials, "Your current password is wrong.");
            }

            user.Password = PasswordHasher.Hash(model.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ResponseVM.Ok(new MessageVM("Password changed."), "Password changed.");
        }

        public async Task<ResponseVM> RefreshTokenAsync(RefreshTokenVM model)
        {
            var hash = HashRefreshToken(model.RefreshToken);
            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.RefreshToken == hash && !x.IsDeleted);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return ResponseVM.Fail(401, ErrorCodes.TokenExpired, "Your session has expired. Please log in again.");
            }

            if (user.IsBlocked)
            {
                return ResponseVM.Fail(403, ErrorCodes.Blocked, "This account is blocked.");
            }

            return ResponseVM.Ok(await StartSessionAsync(user), "Token refreshed.");
        }

        public async Task<ResponseVM> LogoutAsync(long userId)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.Id == userId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await _context.SaveChangesAsync();
            }
            return ResponseVM.Ok(new MessageVM("Logged out."), "Logged out.");
        }

        public async Task<ResponseVM> GetCurrentUserAsync(long userId)
        {
            var user = await _context.AppUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);
            return user == null
                ? ResponseVM.NotFound("User not found.", ErrorCodes.UserNotFound)
                : ResponseVM.Ok(ToUserVM(user));
        }

        // ---- helpers ----

        /// <summary>Issues a new access token and rotates the refresh token (saved hashed).</summary>
        private async Task<AuthSessionVM> StartSessionAsync(AppUser user)
        {
            var refreshToken = CommonMethods.GenerateRefreshToken();
            user.RefreshToken = HashRefreshToken(refreshToken);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.Add(RefreshLifetime);
            user.LastSeen = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new AuthSessionVM
            {
                AccessToken = CommonMethods.GenerateJwtToken(user.Email, user.Id, user.UserName, _config, user.Role),
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(CommonMethods.AccessTokenMinutes(_config)),
                User = ToUserVM(user)
            };
        }

        public static AuthUserVM ToUserVM(AppUser user) => new()
        {
            Id = user.Id.ToString(),
            FullName = user.FullName,
            Email = user.Email,
            Username = user.UserName,
            Role = user.Role,
            EmailVerified = user.IsEmailVerified,
            OnboardingCompleted = user.OnboardingCompleted,
            AvatarUrl = string.IsNullOrWhiteSpace(user.ProfileImageUrl) ? null : user.ProfileImageUrl
        };

        private async Task<(AppUser? User, ResponseVM? Error)> FindByTokenAsync(string token, string email, string purpose)
        {
            var principal = CommonMethods.ValidatePurposeToken(token, purpose, _config, out var expired);
            if (principal == null)
            {
                return (null, expired
                    ? ResponseVM.BadRequest(ErrorCodes.TokenExpired, "This link has expired. Ask for a new one.")
                    : ResponseVM.BadRequest(ErrorCodes.InvalidToken, "This link is not valid."));
            }

            var tokenEmail = principal.FindFirst("Email")?.Value ?? "";
            if (!string.Equals(tokenEmail, NormaliseEmail(email), StringComparison.OrdinalIgnoreCase)
                || !long.TryParse(principal.FindFirst("ID")?.Value, out var userId))
            {
                return (null, ResponseVM.BadRequest(ErrorCodes.InvalidToken, "This link is not valid."));
            }

            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.Id == userId && x.Email == tokenEmail && !x.IsDeleted);
            return user == null
                ? (null, ResponseVM.NotFound("We could not find that account.", ErrorCodes.UserNotFound))
                : (user, null);
        }

        private async Task SendVerifyEmailAsync(AppUser user)
        {
            var token = CommonMethods.GeneratePurposeToken(user.Id, user.Email, VerifyPurpose, VerifyLifetime, _config);
            var link = FrontendLink("verify-email", user.Email, token);
            await SendTemplateAsync(user.Email, "Coddify - Verify your account", "verifyEmail.html", "{{VERIFY_LINK}}", link);
        }

        private async Task SendResetEmailAsync(AppUser user)
        {
            var token = CommonMethods.GeneratePurposeToken(user.Id, user.Email, ResetPurpose, ResetLifetime, _config);
            var link = FrontendLink("reset-password", user.Email, token);
            await SendTemplateAsync(user.Email, "Coddify - Reset your password", "resetEmail.html", "{{RESET_LINK}}", link);
        }

        /// <summary>Email links open the Angular app, which calls the API with the token.</summary>
        private string FrontendLink(string page, string email, string token)
        {
            var baseUrl = (_config["App:FrontendUrl"] ?? "http://localhost:4200").TrimEnd('/');
            return $"{baseUrl}/{page}?email={WebUtility.UrlEncode(email)}&token={WebUtility.UrlEncode(token)}";
        }

        private async Task SendTemplateAsync(string to, string subject, string template, string placeholder, string link)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "emailTemplates", template);
            var body = File.Exists(path)
                ? (await File.ReadAllTextAsync(path)).Replace(placeholder, link)
                : $"<p><a href=\"{link}\">{link}</a></p>";

            var result = await CommonMethods.SendEmail(to, subject, body, _config);
            if (result != "mail sent successfully")
            {
                // Without SMTP (local development) the link is still usable from the log.
                _logger.LogWarning("Email to {Email} not sent ({Result}). Link: {Link}", to, result, link);
            }
        }

        private async Task<string> UniqueUsernameAsync(string fullName, string email)
        {
            var source = string.IsNullOrWhiteSpace(fullName) ? email.Split('@')[0] : fullName;
            var baseName = Regex.Replace(source.ToLowerInvariant(), "[^a-z0-9]", "");
            if (baseName.Length < 3)
            {
                baseName = "coder";
            }
            baseName = baseName.Length > 16 ? baseName[..16] : baseName;

            var username = baseName;
            var counter = 1;
            while (await _context.AppUsers.AnyAsync(x => x.UserName == username && !x.IsDeleted))
            {
                username = $"{baseName}{counter++}";
            }
            return username;
        }

        private static string NormaliseEmail(string email) => email.Trim().ToLowerInvariant();

        private static string HashRefreshToken(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
