using Application.VMs.Gamification;
using Domian.Entities.Gamification;

namespace Application.Interfaces.Gamification
{
    /// <summary>
    /// The one place that changes XP, gems, streaks, daily counters and league XP. Lessons, chests
    /// and solved problems all go through it, so the rules never drift apart.
    /// </summary>
    public interface IRewardService
    {
        /// <summary>The learner's stats row (created on first use) with "today" counters and streak up to date.</summary>
        Task<UserStats> GetStatsAsync(long userId);

        /// <summary>Records a completed activity. Does not call SaveChanges; the caller saves once.</summary>
        Task<ActivityResult> RecordActivityAsync(long userId, int xp, int gems, bool perfect);

        /// <summary>Adds gems without counting an activity (chapter chests).</summary>
        Task AddGemsAsync(long userId, int gems);
    }
}
