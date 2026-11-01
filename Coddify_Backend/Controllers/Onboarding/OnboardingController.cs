using Application.Interfaces.Onboarding;
using Application.VMs.Onboarding;
using Coddify_Backend.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coddify_Backend.Controllers.Onboarding
{
    [Authorize]
    [Route("api/onboarding")]
    public class OnboardingController : BaseApiController
    {
        private readonly IOnboardingService _onboarding;

        public OnboardingController(IOnboardingService onboarding)
        {
            _onboarding = onboarding;
        }

        [HttpPost]
        public async Task<IActionResult> Save(SaveOnboardingVM model) =>
            Respond(await _onboarding.SaveAsync(UserId, model));
    }
}
