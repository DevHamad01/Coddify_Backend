using Application.VMs;
using Application.VMs.Gamification;

namespace Application.Interfaces.Gamification
{
    /// <summary>Cosmetics unlocked by progress (nothing is sold) and what the learner has equipped.</summary>
    public interface IStoreService
    {
        Task<ResponseVM> GetStoreAsync(long userId, string tab);
        Task<ResponseVM> EquipAsync(long userId, string itemKey);

        /// <summary>Every published cosmetic with its state for this learner.</summary>
        Task<List<StoreItemVM>> GetItemsAsync(long userId);
    }
}
