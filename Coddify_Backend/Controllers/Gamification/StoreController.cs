using Application.Interfaces.Gamification;
using Coddify_Backend.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coddify_Backend.Controllers.Gamification
{
    /// <summary>Progress-unlocked cosmetics (editor themes, AI tutors, profile items) and equipping them.</summary>
    [Authorize]
    [Route("api/store")]
    public class StoreController : BaseApiController
    {
        private readonly IStoreService _store;

        public StoreController(IStoreService store)
        {
            _store = store;
        }

        [HttpGet("{tab}")]
        public async Task<IActionResult> Items(string tab) => Respond(await _store.GetStoreAsync(UserId, tab));

        [HttpPost("items/{itemId}/equip")]
        public async Task<IActionResult> Equip(string itemId) => Respond(await _store.EquipAsync(UserId, itemId));
    }
}
