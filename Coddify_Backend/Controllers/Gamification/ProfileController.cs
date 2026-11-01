using Application.Interfaces.Gamification;
using Application.VMs.Gamification;
using Coddify_Backend.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coddify_Backend.Controllers.Gamification
{
    /// <summary>The learner's own profile: details, showcase, avatar, collection and levels.</summary>
    [Authorize]
    [Route("api/me")]
    public class ProfileController : BaseApiController
    {
        private readonly IProfileService _profile;

        public ProfileController(IProfileService profile)
        {
            _profile = profile;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> Get() => Respond(await _profile.GetProfileAsync(UserId));

        [HttpPut("profile")]
        public async Task<IActionResult> Update(ProfileUpdateVM model) => Respond(await _profile.UpdateProfileAsync(UserId, model));

        [HttpPut("profile/showcase")]
        public async Task<IActionResult> Showcase(ShowcaseVM model) => Respond(await _profile.SetShowcaseAsync(UserId, model));

        [HttpPut("profile/avatar")]
        public async Task<IActionResult> Avatar(AvatarLookVM model) => Respond(await _profile.SetAvatarAsync(UserId, model));

        [HttpGet("collection")]
        public async Task<IActionResult> Collection() => Respond(await _profile.GetCollectionAsync(UserId));

        [HttpGet("levels")]
        public async Task<IActionResult> Levels() => Respond(await _profile.GetLevelsAsync(UserId));
    }
}
