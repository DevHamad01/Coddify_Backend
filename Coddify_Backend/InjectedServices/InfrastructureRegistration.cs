using Application.Interfaces.AppUsers;
using Application.Interfaces.CodeExecution;
using Application.Interfaces.Common;
using Application.Interfaces.Gamification;
using Application.Interfaces.Learning;
using Application.Interfaces.Onboarding;
using Application.Interfaces.Practice;
using Application.Interfaces.Tutor;
using Infrastructure.Services.Tutor;
using Infrastructure.Services.Practice;
using Infrastructure.Services.Social;
using Coddify_Backend.Hubs;
using Infrastructure.Context;
using Infrastructure.Seed;
using Infrastructure.Services.Auth.AppUsers;
using Infrastructure.Services.CodeExecution;
using Infrastructure.Services.Common;
using Infrastructure.Services.Gamification;
using Infrastructure.Services.Learning;
using Infrastructure.Services.Onboarding;
using Microsoft.EntityFrameworkCore;

namespace Coddify_Backend.InjectedServices
{
    /// <summary>Database and every Application interface bound to its implementation.</summary>
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
            services.AddScoped<DbSeeder>();

            // Shared
            services.AddSingleton<IAppClock, AppClock>();
            services.AddSingleton<ICodeRunner, MiniCodeRunner>();
            services.AddSignalR();
            services.AddSingleton<ILiveNotifier, SignalRLiveNotifier>();

            // Users
            services.AddScoped<IUserAuthentication, AuthenticationUserService>();
            services.AddScoped<IAdminUserService, AdminUserService>();
            services.AddScoped<IOnboardingService, OnboardingService>();

            // Learning and gamification
            services.AddScoped<IRewardService, RewardService>();
            services.AddScoped<IGamificationService, GamificationService>();
            services.AddScoped<ILearningService, LearningService>();
            services.AddScoped<IStoreService, StoreService>();
            services.AddScoped<ILeaderboardService, LeaderboardService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<ISocialService, SocialService>();

            // Practice (TemporaryProblemJudge is swapped for Judge0 once it is hosted)
            services.AddSingleton<IProblemJudge, TemporaryProblemJudge>();
            services.AddScoped<IPracticeService, PracticeService>();

            // AI tutor (Claude when AI:ApiKey is set, built-in hints otherwise)
            services.AddScoped<ITutorService, TutorService>();

            return services;
        }

        /// <summary>Applies pending migrations and seeds the starter catalog (Database:MigrateOnStartup, on by default).</summary>
        public static async Task InitialiseDatabaseAsync(this WebApplication app)
        {
            if (!app.Configuration.GetValue("Database:MigrateOnStartup", true))
            {
                return;
            }

            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
        }
    }
}
