
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.VMs.AppUsers.Autentication
{
    public class onLoggedInVM
    {
        public long Id { get; set; }
        public string Token { get; set; } = "";
        public string UserName { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string ProfileImageUrl { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public string Bio { get; set; }
    }
}
