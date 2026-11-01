using Application.Interfaces.Gamification;
using Application.VMs;
using Application.VMs.Gamification;
using CommonOperations.Constants.Common;
using Domian.Entities.Gamification;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Social
{
    /// <summary>Follow learners (they show on the Friends leaderboard) and find them by name.</summary>
    public class SocialService : ISocialService
    {
        private readonly AppDbContext _db;

        public SocialService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<ResponseVM> FollowAsync(long userId, string username)
        {
            var target = await FindAsync(username);
            if (target == null)
            {
                return ResponseVM.NotFound("We could not find that learner.", ErrorCodes.UserNotFound);
            }
            if (target == userId)
            {
                return ResponseVM.BadRequest(ErrorCodes.Validation, "You cannot follow yourself.");
            }
            if (!await _db.Follows.AnyAsync(x => x.FollowerId == userId && x.FolloweeId == target))
            {
                _db.Follows.Add(new Follow { FollowerId = userId, FolloweeId = target.Value });
                await _db.SaveChangesAsync();
            }
            return ResponseVM.Ok(null, "Following.");
        }

        public async Task<ResponseVM> UnfollowAsync(long userId, string username)
        {
            var target = await FindAsync(username);
            var follow = target == null
                ? null
                : await _db.Follows.FirstOrDefaultAsync(x => x.FollowerId == userId && x.FolloweeId == target);
            if (follow != null)
            {
                _db.Follows.Remove(follow);
                await _db.SaveChangesAsync();
            }
            return ResponseVM.Ok(null, "Unfollowed.");
        }

        public async Task<ResponseVM> SearchAsync(long userId, string query)
        {
            var q = query?.Trim() ?? "";
            if (q.Length < 2)
            {
                return ResponseVM.Ok(new List<UserSearchResultVM>());
            }

            var following = _db.Follows.Where(x => x.FollowerId == userId).Select(x => x.FolloweeId);
            var results = await _db.AppUsers.AsNoTracking()
                .Where(x => !x.IsDeleted && !x.IsBlocked && x.Role == Roles.Learner && x.Id != userId
                            && (x.UserName.Contains(q) || x.FullName.Contains(q)))
                .OrderBy(x => x.FullName)
                .Take(20)
                .Select(x => new UserSearchResultVM
                {
                    UserId = x.Id.ToString(),
                    FullName = x.FullName,
                    Username = x.UserName,
                    TotalXp = _db.UserStats.Where(s => s.UserId == x.Id).Select(s => s.TotalXp).FirstOrDefault(),
                    FollowedByMe = following.Contains(x.Id)
                })
                .ToListAsync();
            return ResponseVM.Ok(results);
        }

        private async Task<long?> FindAsync(string username) =>
            await _db.AppUsers.Where(x => x.UserName == username && !x.IsDeleted && !x.IsBlocked)
                .Select(x => (long?)x.Id)
                .FirstOrDefaultAsync();
    }
}
