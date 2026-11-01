using Application.VMs;
using Application.VMs.Onboarding;

namespace Application.Interfaces.Onboarding
{
    public interface IOnboardingService
    {
        /// <summary>Saves the answers, marks onboarding done and enrols the learner in the recommended course.</summary>
        Task<ResponseVM> SaveAsync(long userId, SaveOnboardingVM model);
    }
}
