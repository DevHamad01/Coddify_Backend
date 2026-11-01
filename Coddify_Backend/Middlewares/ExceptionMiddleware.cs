using Application.VMs;
using CommonOperations.Constants.Common;

namespace Coddify_Backend.Middlewares
{
    /// <summary>Turns unhandled exceptions into a 500 ResponseVM and logs them, without leaking details.</summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex) when (!context.Response.HasStarted)
            {
                _logger.LogError(ex, "Unhandled error on {Method} {Path}", context.Request.Method, context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(
                    ResponseVM.Fail(500, ErrorCodes.ServerError, "Something went wrong on our side. Please try again."));
            }
        }
    }
}
