using System;
using System.Collections.Generic;
using System.Text;

namespace Application.VMs.AppUsers.Authentication
{
    public class ChangeUserPassowrdVM
    {
        public long UserID { get; set; }
        public string OldPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
    }
}
