using System;
using System.Collections.Generic;
using System.Text;

namespace Application.VMs.AppUsers.Authentication
{
    public class UserVM
    {
        public long Id { get; set; }
        public string FullName { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; } = "";
        public string Gender { get; set; } = "";
        public string ProfileImageUrl { get; set; } = "";
        public string AddedBy { get; set; } = "";
        public DateTime AddedDate { get; set; }
        public string UpdatedBy { get; set; } = "";
        public DateTime UpdatedDate { get; set; }
    }
}

