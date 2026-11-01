using Application.Interfaces.Gamification;
using Application.VMs.Gamification;
using Coddify_Backend.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coddify_Backend.Controllers.Gamification
{
    /// <summary>The learner's stats, daily goal, streak and daily missions.</summary>
    [Authorize]
    [Route("api/me")]
    public class StatsController : BaseApiController
    {
        private readonly IGamificationService _gamification;

        public StatsController(IGamificationService gamification)
        {
            _gamification = gamification;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> Stats() => Respond(await _gamification.GetStatsAsync(UserId));

        [HttpPut("daily-goal")]
        public async Task<IActionResult> DailyGoal(DailyGoalVM model) =>
            Respond(await _gamification.SetDailyGoalAsync(UserId, model));

        [HttpGet("streak")]
        public async Task<IActionResult> Streak() => Respond(await _gamification.GetStreakAsync(UserId));

        [HttpGet("missions")]
        public async Task<IActionResult> Missions() => Respond(await _gamification.GetMissionsAsync(UserId));
    }
}
