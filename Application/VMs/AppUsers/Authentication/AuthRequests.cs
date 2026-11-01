using System.ComponentModel.DataAnnotations;

namespace Application.VMs.AppUsers.Authentication
{
    /// <summary>Request bodies of /api/auth. Names match the frontend's auth.models.ts.</summary>
    public class RegisterVM
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = "";

        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = "";

        [Required, StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = "";
    }

    public class LoginVM
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }

    public class GoogleLoginVM
    {
        [Required]
        public string IdToken { get; set; } = "";
    }

    public class VerifyEmailVM
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Token { get; set; } = "";
    }

    public class EmailVM
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";
    }

    public class ResetPasswordVM
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Token { get; set; } = "";

        [Required, StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; } = "";
    }

    public class RefreshTokenVM
    {
        [Required]
        public string RefreshToken { get; set; } = "";
    }

    public class ChangePasswordVM
    {
        /// <summary>Empty for Google accounts that never set a password.</summary>
        public string OldPassword { get; set; } = "";

        [Required, StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; } = "";

        [Required, Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
