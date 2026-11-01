using Application.Interfaces.Gamification;
using Coddify_Backend.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coddify_Backend.Controllers.Gamification
{
    /// <summary>This week's league and friends boards. Live updates arrive over /hubs/live.</summary>
    [Authorize]
    [Route("api/leaderboard")]
    public class LeaderboardController : BaseApiController
    {
        private readonly ILeaderboardService _leaderboard;

        public LeaderboardController(ILeaderboardService leaderboard)
        {
            _leaderboard = leaderboard;
        }

        [HttpGet("{scope}")]
        public async Task<IActionResult> Get(string scope) => Respond(await _leaderboard.GetLeaderboardAsync(UserId, scope));
    }
}
