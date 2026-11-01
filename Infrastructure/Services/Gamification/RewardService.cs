using Application.Interfaces.Common;
using Application.Interfaces.Gamification;
using Application.VMs.Gamification;
using Domian.Entities.Gamification;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Gamification
{
    public class RewardService : IRewardService
    {
        private readonly AppDbContext _db;
        private readonly IAppClock _clock;

        public RewardService(AppDbContext db, IAppClock clock)
        {
            _db = db;
            _clock = clock;
        }

        public async Task<UserStats> GetStatsAsync(long userId)
        {
            var stats = _db.UserStats.Local.FirstOrDefault(x => x.UserId == userId)
                ?? await _db.UserStats.FirstOrDefaultAsync(x => x.UserId == userId);
            if (stats == null)
            {
                stats = new UserStats { UserId = userId, CounterDay = _clock.Today };
                _db.UserStats.Add(stats);
            }

            var today = _clock.Today;
            if (stats.CounterDay != today)
            {
                stats.CounterDay = today;
                stats.ActivitiesToday = 0;
                stats.XpToday = 0;
                stats.PerfectToday = false;
            }

            // A streak survives until the end of the day after the last activity.
            if (stats.Streak > 0 && stats.LastActiveDay < today.AddDays(-1))
            {
                stats.Streak = 0;
            }
            return stats;
        }

        public async Task<ActivityResult> RecordActivityAsync(long userId, int xp, int gems, bool perfect)
        {
            var stats = await GetStatsAsync(userId);
            var today = _clock.Today;

            var streakIncreased = stats.LastActiveDay != today;
            if (streakIncreased)
            {
                stats.Streak = stats.LastActiveDay == today.AddDays(-1) ? stats.Streak + 1 : 1;
                stats.LongestStreak = Math.Max(stats.LongestStreak, stats.Streak);
                stats.LastActiveDay = today;
            }

            stats.TotalXp += xp;
            stats.Gems += gems;
            stats.ActivitiesToday += 1;
            stats.XpToday += xp;
            stats.PerfectToday |= perfect;
            stats.UpdatedAt = _clock.UtcNow;

            var day = _db.ActivityDays.Local.FirstOrDefault(x => x.UserId == userId && x.Day == today)
                ?? await _db.ActivityDays.FirstOrDefaultAsync(x => x.UserId == userId && x.Day == today);
            if (day == null)
            {
                day = new ActivityDay { UserId = userId, Day = today };
                _db.ActivityDays.Add(day);
            }
            day.Activities += 1;
            day.Xp += xp;

            if (xp > 0)
            {
                var week = _clock.WeekStart;
                var weekly = _db.WeeklyXps.Local.FirstOrDefault(x => x.UserId == userId && x.WeekStart == week)
                    ?? await _db.WeeklyXps.FirstOrDefaultAsync(x => x.UserId == userId && x.WeekStart == week);
                if (weekly == null)
                {
                    weekly = new WeeklyXp { UserId = userId, WeekStart = week };
                    _db.WeeklyXps.Add(weekly);
                }
                weekly.Xp += xp;
            }

            return new ActivityResult(streakIncreased, stats.Streak);
        }

        public async Task AddGemsAsync(long userId, int gems)
        {
            var stats = await GetStatsAsync(userId);
            stats.Gems += gems;
            stats.UpdatedAt = _clock.UtcNow;
        }
    }
}
