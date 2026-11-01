using Application.Interfaces.AppUsers;
using Coddify_Backend.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coddify_Backend.Controllers.AppUsers
{
    /// <summary>The signed-in user (restores the session after a page refresh).</summary>
    [Authorize]
    [Route("api/me")]
    public class MeController : BaseApiController
    {
        private readonly IUserAuthentication _auth;

        public MeController(IUserAuthentication auth)
        {
            _auth = auth;
        }

        [HttpGet]
        public async Task<IActionResult> Get() => Respond(await _auth.GetCurrentUserAsync(UserId));
    }
}
