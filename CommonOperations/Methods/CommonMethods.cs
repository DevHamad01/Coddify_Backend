using Amazon.S3;
using Amazon.S3.Model;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
namespace CommonOperations.Methods
{
    public static class CommonMethods
    {

        private static IAmazonS3 _s3;
        private static IConfiguration _config;


        public static void Intialize(IAmazonS3 s3, IConfiguration config)
        {
            _s3 = s3;
            _config = config;
        }


        #region JWT
        public static String ConvertStringToShah256(string value)
        {
            StringBuilder Sb = new StringBuilder();
            using (var hash = SHA256.Create())
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(value));

                foreach (byte b in result)
                    Sb.Append(b.ToString("x2"));
            }
            return Sb.ToString();
        }
        public static string EncrypthePassword(string Password)
        {
            string password = Base64Encode(Password);
            return ConvertStringToShah256(password);
        }
        public static string Base64Encode(string password)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(password);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        /// <summary>Access token lifetime in minutes (JWT:AccessTokenMinutes, default 60).</summary>
        public static int AccessTokenMinutes(IConfiguration config) =>
            int.TryParse(config["JWT:AccessTokenMinutes"], out var minutes) ? minutes : 60;

        public static string GenerateJwtToken(
         string UserEmail,
         long UserID,
         string UserName,
         IConfiguration config,
         string role = Constants.Common.Roles.Learner)
        {
            var authClaims = new List<Claim>
    {
        new Claim("Email", UserEmail),
        new Claim("ID", UserID.ToString()),
        new Claim("UserName", UserName),
        new Claim(ClaimTypes.Role, role),
        new Claim("purpose", "access")
    };

            return WriteToken(authClaims, DateTime.UtcNow.AddMinutes(AccessTokenMinutes(config)), config);
        }

        /// <summary>
        /// Short-lived signed token for email links ("verify" or "reset"). Unlike the old flow it is
        /// validated (signature, issuer, audience, expiry and purpose) before it is trusted.
        /// </summary>
        public static string GeneratePurposeToken(long userId, string email, string purpose, TimeSpan lifetime, IConfiguration config)
        {
            var claims = new List<Claim>
            {
                new Claim("ID", userId.ToString()),
                new Claim("Email", email),
                new Claim("purpose", purpose)
            };
            return WriteToken(claims, DateTime.UtcNow.Add(lifetime), config);
        }

        /// <summary>Returns the token's claims when it is valid and made for <paramref name="purpose"/>, else null.</summary>
        public static ClaimsPrincipal? ValidatePurposeToken(string token, string purpose, IConfiguration config, out bool expired)
        {
            expired = false;
            try
            {
                var principal = new JwtSecurityTokenHandler().ValidateToken(token, ValidationParameters(config, validateLifetime: true), out _);
                return principal.FindFirst("purpose")?.Value == purpose ? principal : null;
            }
            catch (SecurityTokenExpiredException)
            {
                expired = true;
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static TokenValidationParameters ValidationParameters(IConfiguration config, bool validateLifetime) => new()
        {
            ValidateIssuer = true,
            ValidIssuer = config["JWT:ValidIssuer"],
            ValidateAudience = true,
            ValidAudience = config["JWT:ValidAudience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:Secret"]!)),
            ValidateLifetime = validateLifetime,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = ClaimTypes.Role
        };

        private static string WriteToken(IEnumerable<Claim> claims, DateTime expires, IConfiguration config)
        {
            var authSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["JWT:Secret"]!));

            var token = new JwtSecurityToken(
                issuer: config["JWT:ValidIssuer"],
                audience: config["JWT:ValidAudience"],
                expires: expires,
                claims: claims,
                signingCredentials: new SigningCredentials(
                    authSigningKey,
                    SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public static ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token, IConfiguration config)
        {
            // The access token may be expired here; everything else (signature, issuer, audience) must hold.
            var tokenValidationParameters = ValidationParameters(config, validateLifetime: false);

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }
        public static string GetFileUrl(string filePath)
{
    if (string.IsNullOrWhiteSpace(filePath))
        return "";

    return filePath;
}
        public static string ExtractClaimFromToken(string token, string claimType)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var claim = jwtToken.Claims.FirstOrDefault(c => c.Type == claimType);
                return claim?.Value;
            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region SMTP
        public static async Task<string> SendEmail(string userEmail, string subject, string body, IConfiguration config)
        {
            string response = "";
            var smtpSettings = config.GetSection("SmtpSettings");



            if (string.IsNullOrWhiteSpace(smtpSettings["SenderEmail"]) || string.IsNullOrWhiteSpace(smtpSettings["Password"]))
            {
                return "smtp not configured";
            }

            using (SmtpClient client = new SmtpClient(smtpSettings["Server"], int.Parse(smtpSettings["Port"] ?? "587")))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(smtpSettings["SenderEmail"], smtpSettings["Password"]);
                using MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(smtpSettings["SenderEmail"]!, "Coddify");
                mailMessage.To.Add(userEmail);
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = true;
                try
                {
                    client.EnableSsl = true;
                    await client.SendMailAsync(mailMessage);
                    response = "mail sent successfully";
                }
                catch (Exception ex)
                {
                    response = $"the error is {ex.Message}";
                }
                return response;
            }
        }

        #endregion

    }
}
       
