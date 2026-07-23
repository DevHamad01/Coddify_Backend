using Application.Interfaces.AppUsers;
using Application.VMs.AppUsers.Authentication;
using Microsoft.AspNetCore.Mvc;


namespace Coddify_Backend.Controllers.AppUsers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserAuthenticationController : ControllerBase
    {
        private readonly IUserAuthentication _services;

        public UserAuthenticationController(IUserAuthentication services)
        {
            _services = services;
        }

        [HttpPost("create-user")]
        public IActionResult CreateUser([FromBody] UserVM user)
        {
            var result = _services.CreateUser(user);
            return Ok(result);
        }
        [HttpPost("login-user")]
        public IActionResult LogInUser(LoginUserVM model)
        {

            var result = _services.loginUser(model);
            return Ok(result);
        }


        [HttpPost("ContinueWithGoogle")]
        public async Task<IActionResult> ContinueWithGoogle(string idToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _services.LoginWithGoogle(idToken);
            return Ok(result);
        }


        [HttpGet("VerifyUser")]
        public IActionResult VerifyUser(string token)
        {
            return Ok(_services.VerifyUser(token));
        }

        [HttpPost("ResendVerification")]
        public IActionResult ResendVerification(string email)
        {
            return Ok(_services.ResendVerificationEmail(email));
        }
        [HttpPost("ChangeUserPassword")]
        public IActionResult ChangeUserPassword(ChangeUserPassowrdVM model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = _services.ChangeUserPassword(model);
            return Ok(result);
        }
        [HttpPost("ForgetUserPassword")]
        public IActionResult ForgetUserPassword(string Email)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = _services.ForgetUserPassword(Email);

            return Ok(result);
        }
        [HttpPost("ResetUserPassword")]
        public IActionResult ResetUserPassword(string ResetToken, string password)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = _services.ResetUserPassword(ResetToken, password);
            return Ok(result);
        }
        [HttpPost("UnBlockUser")]
        public IActionResult UnBlockUser(long Id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = _services.UnBlockUser(Id);
            return Ok(result);
        }
        [HttpPost("BlockUser")]
        public IActionResult BlockUser(DeclineUserVM model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = _services.BlockUser(model);
            return Ok(result);
        }

        [HttpPost("DeletUser")]
        public IActionResult DeleteUser(long Id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = _services.DeleteUser(Id);
            return Ok(result);
        }
        [HttpPost("RefreshToken")]
        public IActionResult RefreshToken(TokenApiModel model)
        {
            if (model == null)
                return BadRequest("Invalid client request");

            var result = _services.RefreshToken(model);
            if (result.responseCode != 200)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

    }
}
