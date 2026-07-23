namespace Application.VMs.AppUsers.Authentication
{
    public class TokenApiModel
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
    }
}
