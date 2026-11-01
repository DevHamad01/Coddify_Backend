using Application.Interfaces.AppUsers;
using Application.VMs.AppUsers.Authentication;
using Coddify_Backend.Controllers.Common;
using CommonOperations.Constants.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coddify_Backend.Controllers.Admin
{
    /// <summary>Learner account management for admins.</summary>
    [Authorize(Roles = Roles.Admin)]
    [Route("api/admin/users")]
    public class AdminUsersController : BaseApiController
    {
        private readonly IAdminUserService _users;

        public AdminUsersController(IAdminUserService users)
        {
            _users = users;
        }

        [HttpGet]
        public async Task<IActionResult> Search(string? query, int page = 1, int pageSize = 20) =>
            Respond(await _users.SearchAsync(query, page, pageSize));

        [HttpPost("block")]
        public async Task<IActionResult> Block(DeclineUserVM model) => Respond(await _users.BlockAsync(model));

        [HttpPost("{id:long}/unblock")]
        public async Task<IActionResult> Unblock(long id) => Respond(await _users.UnblockAsync(id));

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id) => Respond(await _users.DeleteAsync(id));
    }
}
