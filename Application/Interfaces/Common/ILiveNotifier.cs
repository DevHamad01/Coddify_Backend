namespace Application.Interfaces.Common
{
    /// <summary>Pushes real-time events to connected browsers (SignalR hub /hubs/live).</summary>
    public interface ILiveNotifier
    {
        /// <summary>A learner's XP changed, so leaderboards should refresh.</summary>
        Task XpChangedAsync(long userId);
    }
}
