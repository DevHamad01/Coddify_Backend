using Application.Interfaces.Learning;
using Application.Interfaces.Onboarding;
using Application.VMs;
using Application.VMs.Onboarding;
using CommonOperations.Constants.Common;
using Domian.Entities.AppUsers;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Onboarding
{
    public class OnboardingService : IOnboardingService
    {
        private readonly AppDbContext _db;
        private readonly ILearningService _learning;

        public OnboardingService(AppDbContext db, ILearningService learning)
        {
            _db = db;
            _learning = learning;
        }

        public async Task<ResponseVM> SaveAsync(long userId, SaveOnboardingVM model)
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);
            if (user == null)
            {
                return ResponseVM.NotFound("User not found.", ErrorCodes.UserNotFound);
            }

            var answers = await _db.UserOnboardings.FirstOrDefaultAsync(x => x.UserId == userId);
            if (answers == null)
            {
                answers = new UserOnboarding { UserId = userId };
                _db.UserOnboardings.Add(answers);
            }
            answers.Goal = model.Goal;
            answers.Language = model.Language;
            answers.Experience = model.Experience;
            answers.DailyMinutes = model.DailyMinutes;
            answers.RecommendedLanguage = model.RecommendedLanguage;
            answers.CompletedAt = DateTime.UtcNow;

            user.OnboardingCompleted = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Start the recommended course; fall back to Python when it isn't in the catalog yet.
            var started = await _learning.StartCourseAsync(userId, model.RecommendedLanguage);
            if (started.responseCode != 200)
            {
                await _learning.StartCourseAsync(userId, "python");
            }

            return ResponseVM.Ok(new OnboardingResultVM(), "Onboarding complete.");
        }
    }
}
