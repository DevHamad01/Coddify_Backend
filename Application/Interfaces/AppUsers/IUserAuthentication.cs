using Application.VMs;
using Application.VMs.AppUsers.Authentication;

namespace Application.Interfaces.AppUsers
{
    public interface IUserAuthentication
    {
        ResponseVM CreateUser(UserVM user);

        ResponseVM loginUser(LoginUserVM model);

        ResponseVM VerifyUser(string token);

        ResponseVM ResendVerificationEmail(string email);

        ResponseVM ChangeUserPassword(ChangeUserPassowrdVM model);
        ResponseVM ResetUserPassword(string resetToken, string password);
        ResponseVM ForgetUserPassword(string email);

        ResponseVM UnBlockUser(long Id);
        ResponseVM DeleteUser(long Id);
        ResponseVM BlockUser(DeclineUserVM model);

        Task<ResponseVM> LoginWithGoogle(string idToken);

        ResponseVM RefreshToken(TokenApiModel tokenApiModel);


    }
}