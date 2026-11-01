namespace Application.VMs.AppUsers.Authentication
{
    /// <summary>The signed-in user as the frontend needs it (auth.models.ts User).</summary>
    public class AuthUserVM
    {
        public string Id { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Username { get; set; } = "";
        public string Role { get; set; } = "";
        public bool EmailVerified { get; set; }
        public bool OnboardingCompleted { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class AuthSessionVM
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";

        /// <summary>When the access token expires (ISO 8601, UTC).</summary>
        public DateTime ExpiresAt { get; set; }

        public AuthUserVM User { get; set; } = new();
    }

    public class RegisterResultVM
    {
        public string Email { get; set; } = "";
        public bool RequiresVerification { get; set; } = true;
    }

    public class MessageVM
    {
        public MessageVM(string message) => Message = message;

        public string Message { get; set; }
    }
}
