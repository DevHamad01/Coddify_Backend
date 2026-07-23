using System;
using System.Collections.Generic;
using System.Text;

namespace Application.VMs.AppUsers.Authentication
{
    public class DeclineUserVM
    {
        public long Id { get; set; }
        public string Reason { get; set; } = "";
    }
}


