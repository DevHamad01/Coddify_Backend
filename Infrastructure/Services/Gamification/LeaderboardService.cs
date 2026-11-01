using Application.Interfaces.Common;
using Application.Interfaces.Gamification;
using Application.VMs;
using Application.VMs.Gamification;
using CommonOperations.Constants.Common;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Gamification
{
    /// <summary>
    /// Weekly boards from WeeklyXp. Everyone is in one "Starter League" for now; tiered leagues with
    /// promotion and demotion can split this later without changing the response shape.
    /// </summary>
    public class LeaderboardService : ILeaderboardService
    {
        private const int BoardSize = 30;

        private readonly AppDbContext _db;
        private readonly IAppClock _clock;

        public LeaderboardService(AppDbContext db, IAppClock clock)
        {
            _db = db;
            _clock = clock;
        }

        public async Task<ResponseVM> GetLeaderboardAsync(long userId, string scope)
        {
            if (scope is not ("league" or "friends"))
            {
                return ResponseVM.NotFound("That leaderboard does not exist.");
            }

            var week = _clock.WeekStart;
            var learners = _db.AppUsers.AsNoTracking().Where(x => !x.IsDeleted && !x.IsBlocked && x.Role == Roles.Learner);
            if (scope == "friends")
            {
                var followees = _db.Follows.Where(x => x.FollowerId == userId).Select(x => x.FolloweeId);
                learners = learners.Where(x => x.Id == userId || followees.Contains(x.Id));
            }

            var rows = await learners
                .Select(user => new
                {
                    user.Id,
                    user.FullName,
                    user.IsPrivate,
                    Xp = _db.WeeklyXps.Where(w => w.UserId == user.Id && w.WeekStart == week).Select(w => w.Xp).FirstOrDefault()
                })
                .Where(x => x.Xp > 0 || x.Id == userId)
                .OrderByDescending(x => x.Xp).ThenBy(x => x.Id)
                .Take(500)
                .ToListAsync();

            var ranked = rows.Select((row, i) => new LeaderboardEntryVM
            {
                Rank = i + 1,
                UserId = row.Id.ToString(),
                Name = row.IsPrivate && row.Id != userId ? "Private learner" : row.FullName,
                Xp = row.Xp,
                IsMe = row.Id == userId,
                IsPrivate = row.IsPrivate
            }).ToList();

            // Show the top of the board, and always include the learner's own row.
            var entries = ranked.Take(BoardSize).ToList();
            var me = ranked.FirstOrDefault(x => x.IsMe);
            if (me != null && !entries.Contains(me))
            {
                entries.Add(me);
            }

            return ResponseVM.Ok(new LeaderboardVM
            {
                Scope = scope,
                LeagueName = "Starter League",
                PromotionCutoff = GamificationRules.PromotionCutoff,
                EndsAt = _clock.WeekEndsUtc,
                Entries = entries
            });
        }
    }
}
