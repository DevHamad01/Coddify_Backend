using Application.Interfaces.Common;
using Microsoft.AspNetCore.SignalR;

namespace Coddify_Backend.Hubs
{
    public class SignalRLiveNotifier : ILiveNotifier
    {
        private readonly IHubContext<LiveHub> _hub;
        private readonly ILogger<SignalRLiveNotifier> _logger;

        public SignalRLiveNotifier(IHubContext<LiveHub> hub, ILogger<SignalRLiveNotifier> logger)
        {
            _hub = hub;
            _logger = logger;
        }

        public async Task XpChangedAsync(long userId)
        {
            try
            {
                await _hub.Clients.Group(LiveHub.LeagueGroup).SendAsync("LeaderboardUpdated", new { userId = userId.ToString() });
            }
            catch (Exception ex)
            {
                // A missed live update must never fail the request that earned the XP.
                _logger.LogWarning(ex, "Could not broadcast LeaderboardUpdated.");
            }
        }
    }
}
