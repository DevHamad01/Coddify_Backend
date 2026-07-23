using Application.Interfaces.AppUsers;
using Application.VMs;
using Application.VMs.AppUsers.Autentication;
using Application.VMs.AppUsers.Authentication;
using CommonOperations.Methods;
using Domian.Entities.AppUsers;
using Infrastructure.Context;
using Infrastructure.Migrations;
using Microsoft.Extensions.Configuration;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Google.Apis.Auth;

namespace Infrastructure.Services.Auth.AppUsers
{
    public class AuthenticationUserService : IUserAuthentication
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;

        public AuthenticationUserService(
            IConfiguration config,
            AppDbContext context)
        {
            _config = config;
            _context = context;
        }

        public ResponseVM CreateUser(UserVM user)
        {
            ResponseVM response = ResponseVM.Instance;

            var existingUser = _context.AppUsers
                .FirstOrDefault(x =>
                    (x.Email == user.Email ||
                     x.UserName == user.UserName) &&
                     !x.IsDeleted);

            if (existingUser != null)
            {
                response.responseCode = 400;
                response.responseMessage = "User already exists";
                return response;
            }

            AppUser newUser = new AppUser
            {
                Email = user.Email,
                UserName = user.UserName,
                Password = CommonMethods.EncrypthePassword(user.Password),
                FullName = user.FullName,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                CreatedAt = DateTime.UtcNow
            };

            _context.AppUsers.Add(newUser);
            _context.SaveChanges();

            response.responseCode = 200;
            response.responseMessage = "User created successfully";

            return response;
        }
        public ResponseVM loginUser(LoginUserVM model)
        {
            ResponseVM response = ResponseVM.Instance;

            var user = _context.AppUsers
                .FirstOrDefault(x => (x.Email == model.Mail || x.UserName == model.Mail) && !x.IsDeleted);

            if (user == null)
            {
                response.responseCode = 400;
                response.responseMessage = "There is no user with this Email or Username, or the account is deleted.";
                return response;
            }

            if (!user.IsEmailVerified)
            {
                var verifyToken = CommonMethods.GenerateJwtToken(
                    user.Email,
                    user.Id,
                    user.UserName,
                   
                    _config
                );

                SendVerifyEmail(verifyToken, user.Email);

                response.responseCode = 400;
                response.responseMessage = "Verification email sent. Please verify your account.";
                return response;
            }

            if (user.IsBlocked)
            {
                response.responseCode = 400;
                response.responseMessage = "Account is Blocked";
                return response;
            }

            if (user.Password != CommonMethods.EncrypthePassword(model.Password))
            {
                response.responseCode = 400;
                response.responseMessage = "Invalid password.";
                return response;
            }

            var refreshToken = CommonMethods.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            _context.SaveChanges();

            var loginData = new onLoggedInVM
            {
                Id = user.Id,
                Token = CommonMethods.GenerateJwtToken(user.Email, user.Id, user.UserName, _config),
                Email = user.Email,
                UserName = user.UserName,
                FullName = user.FullName,
                RefreshToken = refreshToken,
                Bio = user.Bio,
                ProfileImageUrl = CommonMethods.GetFileUrl(user.ProfileImageUrl)
            };

            response.data = loginData;
            response.responseCode = 200;
            response.responseMessage = "Login successful.";

            return response;

        }
        public async Task<ResponseVM> LoginWithGoogle(string idToken)
        {
            ResponseVM response = ResponseVM.Instance;

            try
            {
                var setting = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new List<string> { _config["Google:Client_id"] }
                };

                var res = await GoogleJsonWebSignature.ValidateAsync(idToken, setting);

                if (res == null)
                {
                    response.responseCode = 400;
                    response.responseMessage = "UnVerified by Google";
                    return response;
                }

                var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == res.Email && !u.IsDeleted);

                if (user != null && user.IsBlocked)
                {
                    response.responseCode = 400;
                    response.responseMessage = "Account is Blocked";
                    return response;
                }

                if (user == null)
                {
                    string baseUsername = res.Email.Split('@')[0];
                    string username = baseUsername;
                    int counter = 1;

                    while (await _context.AppUsers.AnyAsync(x => x.UserName == username))
                    {
                        username = $"{baseUsername}{counter}";
                        counter++;
                    }

                    var newUser = new AppUser
                    {
                        FullName = res.Name,
                        Email = res.Email,
                        Password = "",
                        ProfileImageUrl = res.Picture,
                        IsEmailVerified = true,
                        UserName = username,
                        Bio = "",
                        RefreshToken = CommonMethods.GenerateRefreshToken(),
                        RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7),
                        CreatedAt = DateTime.Now
                    };

                    _context.AppUsers.Add(newUser);
                    await _context.SaveChangesAsync();

                    user = newUser;
                }
                else
                {
                    user.RefreshToken = CommonMethods.GenerateRefreshToken();
                    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                    _context.AppUsers.Update(user);
                    await _context.SaveChangesAsync();
                }

                var loginData = new onLoggedInVM
                {
                    Id = user.Id,
                    Token = CommonMethods.GenerateJwtToken(user.Email, user.Id, user.UserName, _config),
                    Email = user.Email,
                    UserName = user.UserName,
                    FullName = user.FullName,
                    ProfileImageUrl = CommonMethods.GetFileUrl(user.ProfileImageUrl),
                    RefreshToken = user.RefreshToken,
                    Bio = user.Bio
                };

                response.responseCode = 200;
                response.responseMessage = "Login successful";
                response.data = loginData;

                return response;
            }
            catch (Google.Apis.Auth.InvalidJwtException)
            {
                response.responseCode = 400;
                response.responseMessage = "Invalid Google ID Token";
                return response;
            }
            catch (Exception ex)
            {
                response.responseCode = 500;
                response.responseMessage = ex.Message;
                return response;
            }
        }






        public ResponseVM VerifyUser(string token)
        {
            ResponseVM response = ResponseVM.Instance;

            var email = CommonMethods.ExtractClaimFromToken(token, "Email");

            if (string.IsNullOrEmpty(email))
            {
                response.responseCode = 400;
                response.responseMessage = "Invalid token";
                return response;
            }

            var user = _context.AppUsers.FirstOrDefault(x =>
                x.Email == email &&
                !x.IsDeleted
            );

            if (user == null)
            {
                response.responseCode = 404;
                response.responseMessage = "User not found";
                return response;
            }

            if (user.IsEmailVerified)
            {
                response.responseCode = 400;
                response.responseMessage = "User already verified";
                return response;
            }

            user.IsEmailVerified = true;
            _context.AppUsers.Update(user);
            _context.SaveChanges();

            var refreshToken = CommonMethods.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            _context.SaveChanges();

            var loginData = new onLoggedInVM
            {
                Id = user.Id,
                Token = CommonMethods.GenerateJwtToken(user.Email, user.Id, user.UserName, _config),
                Email = user.Email,
                UserName = user.UserName,
                FullName = user.FullName,
                RefreshToken = refreshToken,
                Bio = user.Bio,
                ProfileImageUrl = CommonMethods.GetFileUrl(user.ProfileImageUrl)
            };

            response.responseCode = 200;
            response.responseMessage = "Email verified successfully";
            response.data = loginData;

            return response;
        }
        public ResponseVM ResendVerificationEmail(string email)
        {
            ResponseVM response = ResponseVM.Instance;

            var user = _context.AppUsers.FirstOrDefault(x =>
                x.Email == email &&
                !x.IsDeleted
            );

            if (user == null)
            {
                response.responseCode = 404;
                response.responseMessage = "User not found";
                return response;
            }

            if (user.IsEmailVerified)
            {
                response.responseCode = 400;
                response.responseMessage = "User already verified";
                return response;
            }

            var verifyToken = CommonMethods.GenerateJwtToken(
                user.Email,
                user.Id,
                user.UserName,
                _config
            );

           SendVerifyEmail(verifyToken, user.Email);

            response.responseCode = 200;
            response.responseMessage = "Verification email sent successfully";

           return response;
        }

        public ResponseVM ChangeUserPassword(ChangeUserPassowrdVM model)
        {
            ResponseVM response = ResponseVM.Instance;

            if (model.NewPassword != model.ConfirmPassword)
            {
                response.responseCode = 400;
                response.responseMessage = "New password and confirmation password do not match.";
                return response;
            }

            var user = _context.AppUsers.FirstOrDefault(x => x.Id == model.UserID);

            if (user == null)
            {
                response.responseCode = 400;
                response.responseMessage = "User not found.";
                return response;
            }

            if (!string.IsNullOrEmpty(user.Password))
            {
                if (user.Password != CommonMethods.EncrypthePassword(model.OldPassword))
                {
                    response.responseCode = 400;
                    response.responseMessage = "Invalid old password.";
                    return response;
                }
            }

            user.Password = CommonMethods.EncrypthePassword(model.NewPassword);
            _context.SaveChanges();

            response.responseCode = 200;
            response.responseMessage = "Password changed successfully.";
            return response;
        }

        public ResponseVM ResetUserPassword(string resetToken, string password)
        {
            ResponseVM response = ResponseVM.Instance;

            var email = CommonMethods.ExtractClaimFromToken(resetToken, "Email");
            if (email == null)
            {
                response.responseCode = 400;
                response.responseMessage = "Invalid or expired reset token";
                return response;
            }

            var user = _context.AppUsers.FirstOrDefault(x => x.Email == email && !x.IsDeleted);
            if (user == null)
            {
                response.responseCode = 400;
                response.responseMessage = "There is no user with this Email";
                return response;
            }

            if (string.IsNullOrEmpty(user.Password))
            {
                response.responseCode = 400;
                response.responseMessage = "No Password set for this account";
                return response;
            }

            user.Password = CommonMethods.EncrypthePassword(password);
            _context.SaveChanges();

            response.responseCode = 200;
            response.responseMessage = "Password updated successfully";
            return response;
        }
        public ResponseVM ForgetUserPassword(string email)
        {
            ResponseVM response = ResponseVM.Instance;

            var existingMail = _context.AppUsers.FirstOrDefault(e => e.Email == email && !e.IsDeleted);

            if (existingMail == null)
            {
                response.responseCode = 400;
                response.errorMessage = "No User Found with this Email";
                return response;
            }

            if (existingMail.IsBlocked)
            {
                response.responseCode = 400;
                response.responseMessage = "Your account is blocked";
                return response;
            }

            var resetToken = CommonMethods.GenerateJwtToken(existingMail.Email, existingMail.Id, existingMail.UserName, _config);

            SendResendVerifyEmail(resetToken, existingMail.Email);

            response.responseCode = 200;
            response.responseMessage = "Password reset Email sent successfully";

            return response;
        }
        public ResponseVM UnBlockUser(long id)
        {
            ResponseVM response = ResponseVM.Instance;

            var user = _context.AppUsers.FirstOrDefault(x => x.Id == id && !x.IsDeleted);

            if (user == null)
            {
                response.responseCode = 404;
                response.responseMessage = "User not found";
                return response;
            }

            if (!user.IsBlocked)
            {
                response.responseCode = 400;
                response.responseMessage = "User is already unblocked";
                return response;
            }

            user.IsBlocked = false;
            user.BlockedReason = "";
            user.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            response.responseCode = 200;
            response.responseMessage = "User unblocked successfully";

            return response;
        }
        public ResponseVM DeleteUser(long id)
        {
            ResponseVM response = ResponseVM.Instance;

            var user = _context.AppUsers.FirstOrDefault(x => x.Id == id && !x.IsDeleted);

            if (user == null)
            {
                response.responseCode = 404;
                response.responseMessage = "User not found";
                return response;
            }

            user.IsDeleted = true;
            user.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            response.responseCode = 200;
            response.responseMessage = "User deleted successfully";

            return response;
        }
        public ResponseVM BlockUser(DeclineUserVM model)
        {
            ResponseVM response = ResponseVM.Instance;

            var user = _context.AppUsers.FirstOrDefault(x => x.Id == model.Id && !x.IsDeleted);

            if (user == null)
            {
                response.responseCode = 404;
                response.responseMessage = "User not found";
                return response;
            }

            if (user.IsBlocked)
            {
                response.responseCode = 400;
                response.responseMessage = "User is already blocked";
                return response;
            }

            user.IsBlocked = true;
            user.BlockedReason = model.Reason;
            user.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            response.responseCode = 200;
            response.responseMessage = "User blocked successfully";

            return response;
        }

        public ResponseVM RefreshToken(TokenApiModel tokenApiModel)
        {
            ResponseVM response = ResponseVM.Instance;

            if (tokenApiModel == null ||
                string.IsNullOrWhiteSpace(tokenApiModel.AccessToken) ||
                string.IsNullOrWhiteSpace(tokenApiModel.RefreshToken))
            {
                response.responseCode = 400;
                response.responseMessage = "Invalid request.";
                return response;
            }

            var principal = CommonMethods.GetPrincipalFromExpiredToken(
                tokenApiModel.AccessToken,
                _config
            );

            if (principal == null)
            {
                response.responseCode = 400;
                response.responseMessage = "Invalid access token.";
                return response;
            }

            var email = principal.Claims.FirstOrDefault(x => x.Type == "Email")?.Value;

            if (string.IsNullOrEmpty(email))
            {
                response.responseCode = 400;
                response.responseMessage = "Invalid token.";
                return response;
            }

            var user = _context.AppUsers.FirstOrDefault(x =>
                x.Email == email &&
                !x.IsDeleted);

            if (user == null)
            {
                response.responseCode = 404;
                response.responseMessage = "User not found.";
                return response;
            }

            if (user.RefreshToken != tokenApiModel.RefreshToken)
            {
                response.responseCode = 401;
                response.responseMessage = "Invalid refresh token.";
                return response;
            }

            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                response.responseCode = 401;
                response.responseMessage = "Refresh token expired.";
                return response;
            }

            var newAccessToken = CommonMethods.GenerateJwtToken(
                user.Email,
                user.Id,
                user.UserName,
                _config);

            var newRefreshToken = CommonMethods.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            _context.SaveChanges();

            response.responseCode = 200;
            response.responseMessage = "Token refreshed successfully.";
            response.data = new TokenApiModel
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };

            return response;
        }
        private void SendVerifyEmail(string verifyToken, string email)
        {
            string subject = "Codify - Verify your account";
            string link = _config["EmailLinks:VerifyLink"];
            string verifyLink = link.TrimEnd('/') + WebUtility.UrlEncode(verifyToken);

            string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string filePath = Path.Combine(webRootPath, "emailTemplates", "verifyEmail.html");

            string body = System.IO.File.ReadAllText(filePath);
            body = body.Replace("{{VERIFY_LINK}}", verifyLink);

            CommonMethods.SendEmail(email, subject, body, _config);
        }
        private void SendResendVerifyEmail(string verifyToken, string email)
        {
            string subject = "Codify - Reset your password";
            string link = _config["EmailLinks:ResetLink"];
            string verifyLink = link.TrimEnd('/') + WebUtility.UrlEncode(verifyToken);

            string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string filePath = Path.Combine(webRootPath, "emailTemplates", "resetEmail.html");

            string body = System.IO.File.ReadAllText(filePath);
            body = body.Replace("{{RESET_LINK}}", verifyLink);

            CommonMethods.SendEmail(email, subject, body, _config);
        }
    }
    
}
