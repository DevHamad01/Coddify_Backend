using System.Text.Json;
using Application.Interfaces.Common;
using Application.Interfaces.Gamification;
using Application.VMs;
using Application.VMs.Gamification;
using CommonOperations.Constants.Common;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Gamification
{
    public class GamificationService : IGamificationService
    {
        private readonly AppDbContext _db;
        private readonly IRewardService _rewards;
        private readonly IAppClock _clock;

        public GamificationService(AppDbContext db, IRewardService rewards, IAppClock clock)
        {
            _db = db;
            _rewards = rewards;
            _clock = clock;
        }

        public async Task<ResponseVM> GetStatsAsync(long userId)
        {
            var stats = await _rewards.GetStatsAsync(userId);
            await _db.SaveChangesAsync();

            var level = GamificationRules.LevelFor(stats.TotalXp);
            var activeCourse = await _db.Enrollments
                .Where(x => x.UserId == userId && x.IsActive)
                .Join(_db.Courses, enrollment => enrollment.CourseId, course => course.Id, (_, course) => course.Slug)
                .FirstOrDefaultAsync();

            return ResponseVM.Ok(new UserStatsVM
            {
                CourseId = activeCourse ?? "python",
                Streak = stats.Streak,
                Gems = stats.Gems,
                TotalXp = stats.TotalXp,
                Level = level.Level,
                LevelName = level.TierName,
                LevelXp = level.XpIntoLevel,
                LevelXpTarget = level.XpForLevel,
                DailyGoal = stats.DailyGoal,
                ActivitiesToday = stats.ActivitiesToday,
                LeaderboardUnlockXp = GamificationRules.LeaderboardUnlockXp,
                Equipped = Equipped(stats.EquippedJson)
            });
        }

        public async Task<ResponseVM> SetDailyGoalAsync(long userId, DailyGoalVM model)
        {
            if (model.Activities is { } goal && (goal < GamificationRules.MinDailyGoal || goal > GamificationRules.MaxDailyGoal))
            {
                return ResponseVM.BadRequest(ErrorCodes.Validation,
                    $"Pick between {GamificationRules.MinDailyGoal} and {GamificationRules.MaxDailyGoal} activities.");
            }

            var stats = await _rewards.GetStatsAsync(userId);
            stats.DailyGoal = model.Activities;
            stats.UpdatedAt = _clock.UtcNow;
            await _db.SaveChangesAsync();
            return await GetStatsAsync(userId);
        }

        public async Task<ResponseVM> GetStreakAsync(long userId)
        {
            var stats = await _rewards.GetStatsAsync(userId);
            await _db.SaveChangesAsync();

            var since = _clock.Today.AddDays(-400);
            var days = await _db.ActivityDays
                .Where(x => x.UserId == userId && x.Day >= since)
                .OrderBy(x => x.Day)
                .Select(x => x.Day)
                .ToListAsync();

            return ResponseVM.Ok(new StreakSummaryVM
            {
                Streak = stats.Streak,
                LongestStreak = Math.Max(stats.LongestStreak, stats.Streak),
                ActiveToday = stats.ActivitiesToday > 0,
                ActiveDays = days.Select(day => day.ToString("yyyy-MM-dd")).ToList(),
                ClubTarget = GamificationRules.StreakClubTarget
            });
        }

        public async Task<ResponseVM> GetMissionsAsync(long userId)
        {
            var stats = await _rewards.GetStatsAsync(userId);
            await _db.SaveChangesAsync();

            var missions = new List<DailyMissionVM>
            {
                new() { Id = "xp", Title = $"Earn {GamificationRules.XpMissionTarget} XP", Progress = Math.Min(stats.XpToday, GamificationRules.XpMissionTarget), Target = GamificationRules.XpMissionTarget },
                new() { Id = "activity", Title = "Complete one learning activity", Progress = Math.Min(stats.ActivitiesToday, 1), Target = 1 },
                new() { Id = "perfect", Title = "Complete an activity perfectly", Progress = stats.PerfectToday ? 1 : 0, Target = 1 }
            };

            return ResponseVM.Ok(new MissionsSummaryVM
            {
                Missions = missions,
                Marks = missions.Count(mission => mission.Progress >= mission.Target),
                ResetsAt = _clock.NextMidnightUtc,
                Quote = await QuoteOfTheDayAsync()
            });
        }

        /// <summary>A quote pinned to today (AI-generated later), else one from the pool rotating daily.</summary>
        private async Task<QuoteVM> QuoteOfTheDayAsync()
        {
            var today = _clock.Today;
            var quote = await _db.DailyQuotes.AsNoTracking().FirstOrDefaultAsync(x => x.Day == today);
            if (quote == null)
            {
                var pool = await _db.DailyQuotes.AsNoTracking().Where(x => x.Day == null).OrderBy(x => x.Id).ToListAsync();
                quote = pool.Count == 0 ? null : pool[today.DayNumber % pool.Count];
            }
            return quote == null
                ? new QuoteVM { Text = "Learning to code is learning to think differently.", Author = "Bit, Coddify's AI tutor" }
                : new QuoteVM { Text = quote.Text, Author = quote.Author, Likes = quote.Likes };
        }

        /// <summary>Defaults for every slot, overridden by what the learner equipped.</summary>
        public static Dictionary<string, string> Equipped(string equippedJson)
        {
            var result = new Dictionary<string, string>(GamificationRules.DefaultEquipped);
            var chosen = JsonSerializer.Deserialize<Dictionary<string, string>>(string.IsNullOrWhiteSpace(equippedJson) ? "{}" : equippedJson);
            foreach (var (slot, key) in chosen ?? new())
            {
                result[slot] = key;
            }
            return result;
        }
    }
}
