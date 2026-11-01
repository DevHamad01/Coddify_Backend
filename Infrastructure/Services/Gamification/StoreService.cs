using System.Text.Json;
using Application.Interfaces.Gamification;
using Application.VMs;
using Application.VMs.Gamification;
using CommonOperations.Constants.Common;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Gamification
{
    public class StoreService : IStoreService
    {
        private static readonly string[] Tabs = { "themes", "tutors", "profile" };

        private readonly AppDbContext _db;
        private readonly IRewardService _rewards;

        public StoreService(AppDbContext db, IRewardService rewards)
        {
            _db = db;
            _rewards = rewards;
        }

        public async Task<ResponseVM> GetStoreAsync(long userId, string tab)
        {
            if (!Tabs.Contains(tab))
            {
                return ResponseVM.NotFound("That store tab does not exist.");
            }
            var items = await GetItemsAsync(userId);
            return ResponseVM.Ok(items.Where(item => item.Tab == tab).ToList());
        }

        public async Task<ResponseVM> EquipAsync(long userId, string itemKey)
        {
            var items = await GetItemsAsync(userId);
            var item = items.FirstOrDefault(x => x.Id == itemKey);
            if (item == null)
            {
                return ResponseVM.NotFound("That item does not exist.");
            }
            if (item.State == "locked")
            {
                return ResponseVM.BadRequest(ErrorCodes.Locked, "Reach the goal first to unlock that item.");
            }

            var stats = await _rewards.GetStatsAsync(userId);
            var equipped = JsonSerializer.Deserialize<Dictionary<string, string>>(stats.EquippedJson) ?? new();
            equipped[item.Kind] = item.Id;
            stats.EquippedJson = JsonSerializer.Serialize(equipped);
            await _db.SaveChangesAsync();

            items = await GetItemsAsync(userId);
            return ResponseVM.Ok(items.Where(x => x.Tab == item.Tab).ToList(), $"{item.Name} equipped.");
        }

        public async Task<List<StoreItemVM>> GetItemsAsync(long userId)
        {
            var stats = await _rewards.GetStatsAsync(userId);
            await _db.SaveChangesAsync();

            var metrics = new Dictionary<string, int>
            {
                ["xp"] = stats.TotalXp,
                ["level"] = GamificationRules.LevelFor(stats.TotalXp).Level,
                ["streak"] = Math.Max(stats.LongestStreak, stats.Streak),
                ["lessons"] = await _db.NodeCompletions
                    .Where(x => x.UserId == userId)
                    .Join(_db.JourneyNodes.Where(n => n.Kind != "chest"), c => c.NodeId, n => n.Id, (c, _) => c.Id)
                    .CountAsync(),
                ["solved"] = await _db.SolvedProblems.CountAsync(x => x.UserId == userId)
            };
            var equipped = GamificationService.Equipped(stats.EquippedJson);

            var items = await _db.CosmeticItems.AsNoTracking()
                .Where(x => x.IsPublished && !x.IsArchived)
                .OrderBy(x => x.Order)
                .ToListAsync();

            return items.Select(item =>
            {
                var progress = item.UnlockMetric == null ? 0 : metrics.GetValueOrDefault(item.UnlockMetric);
                var unlocked = item.UnlockMetric == null || progress >= item.UnlockTarget;
                return new StoreItemVM
                {
                    Id = item.Key,
                    Tab = item.Tab,
                    Kind = item.Kind,
                    Name = item.Name,
                    Description = item.Description,
                    Art = item.Art,
                    Rarity = item.Rarity,
                    Progress = progress,
                    Unlock = item.UnlockMetric == null ? null : new UnlockRuleVM { Metric = item.UnlockMetric, Target = item.UnlockTarget },
                    State = equipped.GetValueOrDefault(item.Kind) == item.Key ? "equipped" : unlocked ? "unlocked" : "locked"
                };
            }).ToList();
        }
    }
}
