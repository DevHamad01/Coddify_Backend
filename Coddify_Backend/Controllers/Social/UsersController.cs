using Application.Interfaces.Gamification;
using Coddify_Backend.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coddify_Backend.Controllers.Social
{
    /// <summary>Public profiles, following and searching learners.</summary>
    [Authorize]
    [Route("api/users")]
    public class UsersController : BaseApiController
    {
        private readonly IProfileService _profile;
        private readonly ISocialService _social;

        public UsersController(IProfileService profile, ISocialService social)
        {
            _profile = profile;
            _social = social;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string q) => Respond(await _social.SearchAsync(UserId, q));

        [HttpGet("{username}")]
        public async Task<IActionResult> Profile(string username) =>
            Respond(await _profile.GetPublicProfileAsync(UserId, username));

        [HttpPost("{username}/follow")]
        public async Task<IActionResult> Follow(string username) => Respond(await _social.FollowAsync(UserId, username));

        [HttpDelete("{username}/follow")]
        public async Task<IActionResult> Unfollow(string username) => Respond(await _social.UnfollowAsync(UserId, username));
    }
}
