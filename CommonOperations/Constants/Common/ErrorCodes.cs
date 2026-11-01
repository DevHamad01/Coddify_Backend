namespace CommonOperations.Constants.Common
{
    /// <summary>Stable error keys returned in ResponseVM.code. The frontend's ApiErrorCode uses the same strings.</summary>
    public static class ErrorCodes
    {
        public const string EmailTaken = "email_taken";
        public const string InvalidCredentials = "invalid_credentials";
        public const string EmailNotVerified = "email_not_verified";
        public const string InvalidToken = "invalid_token";
        public const string TokenExpired = "token_expired";
        public const string UserNotFound = "user_not_found";
        public const string NotFound = "not_found";
        public const string Validation = "validation";
        public const string Unauthorized = "unauthorized";
        public const string Forbidden = "forbidden";
        public const string Blocked = "account_blocked";
        public const string Locked = "locked";
        public const string Conflict = "conflict";
        public const string GoogleNotConfigured = "google_not_configured";
        public const string ServerError = "server_error";
    }
}
