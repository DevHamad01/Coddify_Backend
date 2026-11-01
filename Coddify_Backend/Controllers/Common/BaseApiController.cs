using Application.VMs;
using Microsoft.AspNetCore.Mvc;

namespace Coddify_Backend.Controllers.Common
{
    /// <summary>
    /// Shared helpers: every endpoint returns a ResponseVM whose responseCode is also the HTTP status,
    /// and signed-in endpoints read the user id from the JWT "ID" claim.
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        protected long UserId =>
            long.TryParse(User.FindFirst("ID")?.Value, out var id) ? id : 0;

        protected IActionResult Respond(ResponseVM response) =>
            StatusCode(response.responseCode == 0 ? 200 : response.responseCode, response);
    }
}
