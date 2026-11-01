namespace Application.Interfaces.Common
{
    /// <summary>"Today" for streaks and daily missions, in the app's time zone (App:TimeZone, default Asia/Karachi).</summary>
    public interface IAppClock
    {
        DateTime UtcNow { get; }
        DateOnly Today { get; }

        /// <summary>Monday of the current league week.</summary>
        DateOnly WeekStart { get; }

        /// <summary>UTC instant of the next local midnight (when daily missions reset).</summary>
        DateTime NextMidnightUtc { get; }

        /// <summary>UTC instant when the current league week ends (Sunday 23:59 local).</summary>
        DateTime WeekEndsUtc { get; }
    }
}
