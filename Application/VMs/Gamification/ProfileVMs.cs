using System.ComponentModel.DataAnnotations;

namespace Application.VMs.Gamification
{
    // Shapes match the frontend's gamification.models.ts (camelCase JSON).

    public class UnlockRuleVM
    {
        public string Metric { get; set; } = "";
        public int Target { get; set; }
    }

    public class StoreItemVM
    {
        public string Id { get; set; } = "";
        public string Tab { get; set; } = "";
        public string Kind { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";

        /// <summary>equipped | unlocked | locked</summary>
        public string State { get; set; } = "locked";

        public UnlockRuleVM? Unlock { get; set; }
        public int Progress { get; set; }
        public string Art { get; set; } = "";
        public string Rarity { get; set; } = "common";
    }

    public class LeaderboardEntryVM
    {
        public int Rank { get; set; }
        public string UserId { get; set; } = "";
        public string Name { get; set; } = "";
        public int Xp { get; set; }
        public bool IsMe { get; set; }
        public bool IsPrivate { get; set; }
    }

    public class LeaderboardVM
    {
        /// <summary>league | friends</summary>
        public string Scope { get; set; } = "league";
        public string LeagueName { get; set; } = "";
        public int PromotionCutoff { get; set; }
        public DateTime EndsAt { get; set; }
        public List<LeaderboardEntryVM> Entries { get; set; } = new();
    }

    public class AvatarLookVM
    {
        [Required, RegularExpression("^#[0-9A-Fa-f]{6}$")]
        public string Skin { get; set; } = "#F1C27D";

        [Required, RegularExpression("^(short|curly|long|bun|none)$")]
        public string Hair { get; set; } = "short";

        [Required, RegularExpression("^#[0-9A-Fa-f]{6}$")]
        public string HairColor { get; set; } = "#4B2E1E";

        [Required, RegularExpression("^#[0-9A-Fa-f]{6}$")]
        public string Shirt { get; set; } = "#14B8A6";

        public bool Glasses { get; set; }

        /// <summary>A hex colour or a linear-gradient() of hex colours.</summary>
        [Required, MaxLength(120), RegularExpression(@"^(#[0-9A-Fa-f]{6}|linear-gradient\(\d{1,3}deg(, #[0-9A-Fa-f]{6}){2,3}\))$")]
        public string Background { get; set; } = "linear-gradient(135deg, #14B8A6, #38BDF8)";
    }

    public class BadgeVM
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int Progress { get; set; }
        public int Target { get; set; }
        public int Tier { get; set; }
        public int MaxTier { get; set; }
    }

    public class ProfileSummaryVM
    {
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? Username { get; set; }
        public string Bio { get; set; } = "";
        public Dictionary<string, string> Socials { get; set; } = new();
        public AvatarLookVM Avatar { get; set; } = new();
        public string Title { get; set; } = "";
        public Dictionary<string, string> Equipped { get; set; } = new();
        public List<StoreItemVM> Showcase { get; set; } = new();
        public int Following { get; set; }
        public int Followers { get; set; }
        public int Streak { get; set; }
        public int TotalXp { get; set; }
        public string? League { get; set; }
        public int Level { get; set; }
        public string LevelName { get; set; } = "";
        public int CollectionUnlocked { get; set; }
        public int CollectionTotal { get; set; }
        public int HistoryUnlockXp { get; set; }
        public List<BadgeVM> Badges { get; set; } = new();
        public string JoinedAt { get; set; } = "";
    }

    public class ProfileUpdateVM
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = "";

        /// <summary>3-20 letters and numbers, or null/empty to keep the generated one.</summary>
        [RegularExpression("^[a-zA-Z0-9]{3,20}$", ErrorMessage = "Usernames are 3 to 20 letters and numbers, with no spaces.")]
        public string? Username { get; set; }

        [MaxLength(300)]
        public string Bio { get; set; } = "";

        public Dictionary<string, string> Socials { get; set; } = new();
    }

    public class ShowcaseVM
    {
        [MaxLength(5)]
        public List<string> ItemIds { get; set; } = new();
    }

    public class LevelStepVM
    {
        public int Level { get; set; }
        public string Name { get; set; } = "";
        public int TotalXp { get; set; }
        public List<string> Rewards { get; set; } = new();
        public bool Reached { get; set; }
    }

    public class LevelTierVM
    {
        public string Name { get; set; } = "";
        public int From { get; set; }
        public int To { get; set; }
        public List<LevelStepVM> Levels { get; set; } = new();
    }

    public class LevelsSummaryVM
    {
        public int Level { get; set; }
        public string TierName { get; set; } = "";
        public int TotalXp { get; set; }
        public int XpIntoLevel { get; set; }
        public int XpForLevel { get; set; }
        public string NextLevelName { get; set; } = "";
        public List<LevelTierVM> Tiers { get; set; } = new();
    }

    /// <summary>What anyone can see on /api/users/{username}.</summary>
    public class PublicProfileVM
    {
        public string UserId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Username { get; set; } = "";
        public bool IsPrivate { get; set; }
        public string Bio { get; set; } = "";
        public Dictionary<string, string> Socials { get; set; } = new();
        public AvatarLookVM Avatar { get; set; } = new();
        public string Title { get; set; } = "";
        public Dictionary<string, string> Equipped { get; set; } = new();
        public List<StoreItemVM> Showcase { get; set; } = new();
        public int Streak { get; set; }
        public int TotalXp { get; set; }
        public int Level { get; set; }
        public string LevelName { get; set; } = "";
        public int Followers { get; set; }
        public int Following { get; set; }
        public bool FollowedByMe { get; set; }
    }

    public class UserSearchResultVM
    {
        public string UserId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Username { get; set; } = "";
        public int TotalXp { get; set; }
        public bool FollowedByMe { get; set; }
    }
}
