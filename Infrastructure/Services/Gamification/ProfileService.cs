using System.Text.Json;
using Application.Interfaces.Gamification;
using Application.VMs;
using Application.VMs.Gamification;
using CommonOperations.Constants.Common;
using Domian.Entities.AppUsers;
using Domian.Entities.Gamification;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Gamification
{
    public class ProfileService : IProfileService
    {
        private static readonly string[] SocialNetworks = { "linkedin", "github", "twitter", "facebook", "instagram", "youtube", "website" };
        private static readonly JsonSerializerOptions CamelCase = new(JsonSerializerDefaults.Web);

        private readonly AppDbContext _db;
        private readonly IRewardService _rewards;
        private readonly IStoreService _store;

        public ProfileService(AppDbContext db, IRewardService rewards, IStoreService store)
        {
            _db = db;
            _rewards = rewards;
            _store = store;
        }

        public async Task<ResponseVM> GetProfileAsync(long userId)
        {
            var user = await _db.AppUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);
            if (user == null)
            {
                return ResponseVM.NotFound("User not found.", ErrorCodes.UserNotFound);
            }

            var stats = await _rewards.GetStatsAsync(userId);
            await _db.SaveChangesAsync();
            var items = await _store.GetItemsAsync(userId);
            var equipped = GamificationService.Equipped(stats.EquippedJson);
            var level = GamificationRules.LevelFor(stats.TotalXp);

            return ResponseVM.Ok(new ProfileSummaryVM
            {
                UserId = user.Id.ToString(),
                Email = user.Email,
                FullName = user.FullName,
                Username = user.UserName,
                Bio = user.Bio,
                Socials = Socials(user),
                Avatar = Avatar(user),
                Title = items.FirstOrDefault(x => x.Id == equipped.GetValueOrDefault("title"))?.Name ?? "",
                Equipped = equipped,
                Showcase = Showcase(stats, items),
                Following = await _db.Follows.CountAsync(x => x.FollowerId == userId),
                Followers = await _db.Follows.CountAsync(x => x.FolloweeId == userId),
                Streak = stats.Streak,
                TotalXp = stats.TotalXp,
                League = stats.TotalXp >= GamificationRules.LeaderboardUnlockXp ? "Starter League" : null,
                Level = level.Level,
                LevelName = level.TierName,
                CollectionUnlocked = items.Count(x => x.State != "locked"),
                CollectionTotal = items.Count,
                HistoryUnlockXp = GamificationRules.LeaderboardUnlockXp,
                Badges = await BadgesAsync(userId, stats),
                JoinedAt = user.CreatedAt.ToString("yyyy-MM-dd")
            });
        }

        public async Task<ResponseVM> UpdateProfileAsync(long userId, ProfileUpdateVM model)
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);
            if (user == null)
            {
                return ResponseVM.NotFound("User not found.", ErrorCodes.UserNotFound);
            }

            var username = model.Username?.Trim();
            if (!string.IsNullOrEmpty(username) && !string.Equals(username, user.UserName, StringComparison.OrdinalIgnoreCase)
                && await _db.AppUsers.AnyAsync(x => x.UserName == username && x.Id != userId && !x.IsDeleted))
            {
                var taken = ResponseVM.Fail(409, "username_taken", "That username is already taken.");
                taken.errors = new() { ["username"] = new[] { "That username is already taken." } };
                return taken;
            }

            var socials = new Dictionary<string, string>();
            foreach (var (network, url) in model.Socials)
            {
                var value = url?.Trim() ?? "";
                if (value.Length == 0)
                {
                    continue;
                }
                if (!SocialNetworks.Contains(network)
                    || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
                    || value.Length > 200)
                {
                    var invalid = ResponseVM.BadRequest(ErrorCodes.Validation, "One of your links is not a valid web address.");
                    invalid.errors = new() { [$"socials.{network}"] = new[] { "Enter a full link starting with https://" } };
                    return invalid;
                }
                socials[network] = value;
            }

            user.FullName = model.FullName.Trim();
            if (!string.IsNullOrEmpty(username))
            {
                user.UserName = username;
            }
            user.Bio = model.Bio.Trim();
            user.SocialsJson = JsonSerializer.Serialize(socials);
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return await GetProfileAsync(userId);
        }

        public async Task<ResponseVM> SetShowcaseAsync(long userId, ShowcaseVM model)
        {
            var unlocked = (await _store.GetItemsAsync(userId)).Where(x => x.State != "locked").Select(x => x.Id).ToHashSet();
            var picks = model.ItemIds.Where(unlocked.Contains).Distinct().Take(GamificationRules.ShowcaseLimit).ToList();

            var stats = await _rewards.GetStatsAsync(userId);
            stats.ShowcaseJson = JsonSerializer.Serialize(picks);
            await _db.SaveChangesAsync();
            return await GetProfileAsync(userId);
        }

        public async Task<ResponseVM> SetAvatarAsync(long userId, AvatarLookVM model)
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);
            if (user == null)
            {
                return ResponseVM.NotFound("User not found.", ErrorCodes.UserNotFound);
            }
            user.AvatarJson = JsonSerializer.Serialize(model, CamelCase);
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return await GetProfileAsync(userId);
        }

        public async Task<ResponseVM> GetCollectionAsync(long userId) =>
            ResponseVM.Ok(await _store.GetItemsAsync(userId));

        public async Task<ResponseVM> GetLevelsAsync(long userId)
        {
            var stats = await _rewards.GetStatsAsync(userId);
            await _db.SaveChangesAsync();
            var current = GamificationRules.LevelFor(stats.TotalXp);

            var rewards = await _db.CosmeticItems.AsNoTracking()
                .Where(x => x.UnlockMetric == "level" && x.IsPublished && !x.IsArchived)
                .Select(x => new { x.UnlockTarget, x.Name })
                .ToListAsync();

            return ResponseVM.Ok(new LevelsSummaryVM
            {
                Level = current.Level,
                TierName = current.TierName,
                TotalXp = stats.TotalXp,
                XpIntoLevel = current.XpIntoLevel,
                XpForLevel = current.XpForLevel,
                NextLevelName = current.Level < GamificationRules.LevelNames.Length
                    ? GamificationRules.LevelNames[current.Level]
                    : "Max level",
                Tiers = GamificationRules.Tiers.Select(tier => new LevelTierVM
                {
                    Name = tier.Name,
                    From = tier.From,
                    To = tier.To,
                    Levels = Enumerable.Range(tier.From, tier.To - tier.From + 1).Select(level => new LevelStepVM
                    {
                        Level = level,
                        Name = GamificationRules.LevelNames[level - 1],
                        TotalXp = GamificationRules.XpToReach(level),
                        Rewards = rewards.Where(x => x.UnlockTarget == level).Select(x => x.Name).ToList(),
                        Reached = level <= current.Level
                    }).ToList()
                }).ToList()
            });
        }

        public async Task<ResponseVM> GetPublicProfileAsync(long viewerId, string username)
        {
            var user = await _db.AppUsers.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserName == username && !x.IsDeleted && !x.IsBlocked);
            if (user == null)
            {
                return ResponseVM.NotFound("We could not find that learner.", ErrorCodes.UserNotFound);
            }

            var stats = await _db.UserStats.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == user.Id) ?? new UserStats();
            var level = GamificationRules.LevelFor(stats.TotalXp);
            var profile = new PublicProfileVM
            {
                UserId = user.Id.ToString(),
                FullName = user.FullName,
                Username = user.UserName,
                IsPrivate = user.IsPrivate,
                Avatar = Avatar(user),
                Level = level.Level,
                LevelName = level.TierName,
                Followers = await _db.Follows.CountAsync(x => x.FolloweeId == user.Id),
                Following = await _db.Follows.CountAsync(x => x.FollowerId == user.Id),
                FollowedByMe = await _db.Follows.AnyAsync(x => x.FollowerId == viewerId && x.FolloweeId == user.Id)
            };

            // Private profiles only show the name, avatar and level to other learners.
            if (!user.IsPrivate || user.Id == viewerId)
            {
                var items = await _store.GetItemsAsync(user.Id);
                var equipped = GamificationService.Equipped(stats.EquippedJson);
                profile.Bio = user.Bio;
                profile.Socials = Socials(user);
                profile.Equipped = equipped;
                profile.Title = items.FirstOrDefault(x => x.Id == equipped.GetValueOrDefault("title"))?.Name ?? "";
                profile.Showcase = Showcase(stats, items);
                profile.Streak = stats.Streak;
                profile.TotalXp = stats.TotalXp;
            }
            return ResponseVM.Ok(profile);
        }

        // ---- helpers ----

        private static Dictionary<string, string> Socials(AppUser user) =>
            string.IsNullOrWhiteSpace(user.SocialsJson)
                ? new()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(user.SocialsJson) ?? new();

        private static AvatarLookVM Avatar(AppUser user) =>
            string.IsNullOrWhiteSpace(user.AvatarJson)
                ? new AvatarLookVM()
                : JsonSerializer.Deserialize<AvatarLookVM>(user.AvatarJson, CamelCase) ?? new AvatarLookVM();

        private static List<StoreItemVM> Showcase(UserStats stats, List<StoreItemVM> items)
        {
            var keys = JsonSerializer.Deserialize<List<string>>(string.IsNullOrWhiteSpace(stats.ShowcaseJson) ? "[]" : stats.ShowcaseJson) ?? new();
            return keys.Select(key => items.FirstOrDefault(x => x.Id == key))
                .Where(item => item != null && item.State != "locked")
                .Select(item => item!)
                .ToList();
        }

        private async Task<List<BadgeVM>> BadgesAsync(long userId, UserStats stats)
        {
            var completions = await _db.NodeCompletions
                .Where(x => x.UserId == userId)
                .Join(_db.JourneyNodes, c => c.NodeId, n => n.Id, (_, n) => n.Kind)
                .ToListAsync();
            var lessons = completions.Count(kind => kind != "chest");
            var chests = completions.Count(kind => kind == "chest");
            var bestStreak = Math.Max(stats.LongestStreak, stats.Streak);

            BadgeVM Badge(string id, string title, string description, int progress, int target, int maxTier) => new()
            {
                Id = id,
                Title = title,
                Description = description,
                Progress = Math.Min(progress, target),
                Target = target,
                Tier = progress >= target ? 1 : 0,
                MaxTier = maxTier
            };

            return new List<BadgeVM>
            {
                Badge("xp-beginner", "Experience Beginner", "Earn 100 XP", stats.TotalXp, 100, 4),
                Badge("daily-coder", "Daily Coder", "Keep a 3-day streak", bestStreak, 3, 5),
                Badge("happy-user", "Happy User", "Use Coddify for 5 days", await _db.ActivityDays.CountAsync(x => x.UserId == userId), 5, 5),
                Badge("first-steps", "First Steps", "Complete 5 lessons", lessons, 5, 3),
                Badge("collector", "Collector", "Open 3 chapter chests", chests, 3, 3),
                Badge("problem-solver", "Problem Solver", "Solve 5 practice problems", await _db.SolvedProblems.CountAsync(x => x.UserId == userId), 5, 4)
            };
        }
    }
}
