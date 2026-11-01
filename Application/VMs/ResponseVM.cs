namespace Application.VMs
{
    /// <summary>
    /// Envelope every API returns. <c>responseCode</c> is also used as the HTTP status code, and
    /// <c>code</c> is a stable error key the frontend branches on (e.g. "email_taken").
    /// </summary>
    public sealed class ResponseVM
    {
        private ResponseVM()
        {
        }

        /// <summary>A fresh response (kept for the existing auth code style).</summary>
        public static ResponseVM Instance => new();

        public int responseCode { get; set; }
        public string code { get; set; } = "";
        public string errorMessage { get; set; } = "";
        public string responseMessage { get; set; } = "";
        public dynamic? data { get; set; }

        /// <summary>Field-level validation messages, e.g. { "email": ["Email is required"] }.</summary>
        public Dictionary<string, string[]>? errors { get; set; }

        public static ResponseVM Ok(object? data = null, string message = "Success") => new()
        {
            responseCode = 200,
            responseMessage = message,
            data = data,
        };

        public static ResponseVM Fail(int status, string code, string message) => new()
        {
            responseCode = status,
            code = code,
            responseMessage = message,
            errorMessage = message,
        };

        public static ResponseVM BadRequest(string code, string message) => Fail(400, code, message);

        public static ResponseVM NotFound(string message, string code = "not_found") =>
            Fail(404, code, message);
    }
}
