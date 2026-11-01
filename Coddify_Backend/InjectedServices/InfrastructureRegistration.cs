using Application.Interfaces.AppUsers;
using Infrastructure.Context;
using Infrastructure.Services.Auth.AppUsers;
using Microsoft.EntityFrameworkCore;

namespace Coddify_Backend.InjectedServices
{
    /// <summary>Database and every Application interface bound to its Infrastructure service.</summary>
    public static class InfrastructureRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    config.GetConnectionString("ConnectionString"),
                    sqlOptions => sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null)));

            // Users
            services.AddScoped<IUserAuthentication, AuthenticationUserService>();
            services.AddScoped<IAdminUserService, AdminUserService>();

            return services;
        }
    }
}
