using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domian.Entities.AppUsers
{
    public class AppUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column(TypeName = "NVARCHAR")]
        [MaxLength(450)]
        public string Email { get; set; } = "";

        [Column(TypeName = "NVARCHAR")]
        [MaxLength(200)]
        public string UserName { get; set; } = "";

        [Column(TypeName = "NVARCHAR")]
        [MaxLength(200)]
        public string Password { get; set; } = "";

        [Column(TypeName = "NVARCHAR")]
        [MaxLength(100)]
        public string FullName { get; set; } = "";

        [Column(TypeName = "NVARCHAR")]
        [MaxLength(100)]
        public string Gender { get; set; } = "";

        [DataType(DataType.DateTime)]
        public DateTime DateOfBirth { get; set; }

        [Column(TypeName = "NVARCHAR")]
        [MaxLength(350)]
        public string Bio { get; set; } = "";

        [Column(TypeName = "NVARCHAR")]
        [MaxLength(200)]
        public string Location { get; set; } = "";

        [Column(TypeName = "NVARCHAR")]
        [MaxLength(1000)]
        public string ProfileImageUrl { get; set; } = "";

        [Column(TypeName = "NVARCHAR")]
        [MaxLength(1000)]
        public string CoverImageUrl { get; set; } = "";

        [Column(TypeName = "NVARCHAR")]
        [MaxLength(1500)]
        public string BlockedReason { get; set; } = "";

        /// <summary>Learner or Admin (see CommonOperations Roles).</summary>
        [Column(TypeName = "NVARCHAR")]
        [MaxLength(20)]
        public string Role { get; set; } = "Learner";

        public bool OnboardingCompleted { get; set; } = false;

        /// <summary>Avatar builder choices as JSON (skin, hair, shirt…); null = default look.</summary>
        public string? AvatarJson { get; set; }

        /// <summary>Profile social links as JSON ({ "github": "https://…" }).</summary>
        [MaxLength(2000)]
        public string? SocialsJson { get; set; }

        public bool IsEmailVerified { get; set; } = false;

        public bool IsBlocked { get; set; } = false;

        public bool IsPrivate { get; set; } = false;

        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiryTime { get; set; }

        public DateTime? LastSeen { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
