using Application.Interfaces.Tutor;
using Application.VMs.Tutor;
using Coddify_Backend.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coddify_Backend.Controllers.Tutor
{
    [Authorize]
    [Route("api/tutor")]
    [EnableRateLimiting("tutor")]
    public class TutorController : BaseApiController
    {
        private readonly ITutorService _tutor;

        public TutorController(ITutorService tutor)
        {
            _tutor = tutor;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat(TutorRequestVM request, CancellationToken cancellationToken) =>
            Respond(await _tutor.AskAsync(UserId, request, cancellationToken));
    }
}
