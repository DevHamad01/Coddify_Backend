using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Coddify_Backend.Hubs
{
    /// <summary>
    /// Real-time channel at /hubs/live. Every signed-in learner joins the league group; the server
    /// sends "LeaderboardUpdated" when someone's XP changes. Notifications will use the same hub.
    /// </summary>
    [Authorize]
    public class LiveHub : Hub
    {
        public const string Path = "/hubs/live";
        public const string LeagueGroup = "league";

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, LeagueGroup);
            await base.OnConnectedAsync();
        }
    }
}
