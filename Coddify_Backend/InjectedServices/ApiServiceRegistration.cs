using System.Threading.RateLimiting;
using Application.VMs;
using CommonOperations.Constants.Common;
using CommonOperations.Methods;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

namespace Coddify_Backend.InjectedServices
{
    /// <summary>Web-layer setup: JWT auth, CORS, rate limits, validation responses and Swagger.</summary>
    public static class ApiServiceRegistration
    {
        public const string CorsPolicy = "frontend";

        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddControllers().ConfigureApiBehaviorOptions(options =>
            {
                // Model validation errors use the same ResponseVM envelope as every other answer.
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(entry => entry.Value?.Errors.Count > 0)
                        .ToDictionary(
                            entry => ToCamelCase(entry.Key),
                            entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());
                    var response = ResponseVM.BadRequest(ErrorCodes.Validation, "Some details look off. Please check the form.");
                    response.errors = errors;
                    return new BadRequestObjectResult(response);
                };
            });

            if (string.IsNullOrWhiteSpace(config["JWT:Secret"]) || config["JWT:Secret"]!.Length < 32)
            {
                throw new InvalidOperationException(
                    "JWT:Secret is missing or shorter than 32 characters. Set it with 'dotnet user-secrets set JWT:Secret <value>'.");
            }

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = CommonMethods.ValidationParameters(config, validateLifetime: true);
                    options.Events = new JwtBearerEvents
                    {
                        // Browsers cannot send headers on WebSockets, so SignalR passes the token in the query string.
                        OnMessageReceived = context =>
                        {
                            var token = context.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            {
                                context.Token = token;
                            }
                            return Task.CompletedTask;
                        },
                        // Email-link tokens are signed with the same key; only access tokens may sign in.
                        OnTokenValidated = context =>
                        {
                            if (context.Principal?.FindFirst("purpose")?.Value != "access")
                            {
                                context.Fail("Not an access token.");
                            }
                            return Task.CompletedTask;
                        },
                        OnChallenge = async context =>
                        {
                            context.HandleResponse();
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsJsonAsync(
                                ResponseVM.Fail(401, ErrorCodes.Unauthorized, "Please log in to continue."));
                        },
                        OnForbidden = async context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            await context.Response.WriteAsJsonAsync(
                                ResponseVM.Fail(403, ErrorCodes.Forbidden, "You do not have access to this."));
                        }
                    };
                });
            services.AddAuthorization();

            var origins = config.GetSection("App:CorsOrigins").Get<string[]>()
                ?? new[] { "http://localhost:4200", "http://localhost:4300" };
            services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()));

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, token) =>
                    await context.HttpContext.Response.WriteAsJsonAsync(
                        ResponseVM.Fail(429, "rate_limited", "Too many requests. Please wait a moment."), token);

                // Per client IP for sign-in endpoints, per user for code runs and the AI tutor.
                options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "anon",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
                options.AddPolicy("run", context => RateLimitPartition.GetFixedWindowLimiter(
                    context.User.FindFirst("ID")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anon",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));
                options.AddPolicy("tutor", context => RateLimitPartition.GetFixedWindowLimiter(
                    context.User.FindFirst("ID")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anon",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 15, Window = TimeSpan.FromMinutes(1) }));
            });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Coddify API", Version = "v1" });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Paste the accessToken from /api/auth/login."
                });
                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                });
            });

            return services;
        }

        private static string ToCamelCase(string key)
        {
            var name = key.StartsWith("$.") ? key[2..] : key;
            return string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
        }
    }
}
