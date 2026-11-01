namespace Application.VMs.Gamification
{
    // Shapes match the frontend's gamification.models.ts (camelCase JSON).

    public class UserStatsVM
    {
        public string CourseId { get; set; } = "";
        public int Streak { get; set; }
        public int Gems { get; set; }
        public int TotalXp { get; set; }
        public int Level { get; set; }
        public string LevelName { get; set; } = "";
        public int LevelXp { get; set; }
        public int LevelXpTarget { get; set; }
        public int? DailyGoal { get; set; }
        public int ActivitiesToday { get; set; }
        public int LeaderboardUnlockXp { get; set; }
        public Dictionary<string, string> Equipped { get; set; } = new();
    }

    public class DailyGoalVM
    {
        /// <summary>2-12 activities, or null to remove the goal.</summary>
        public int? Activities { get; set; }
    }

    public class StreakSummaryVM
    {
        public int Streak { get; set; }
        public int LongestStreak { get; set; }
        public bool ActiveToday { get; set; }
        public List<string> ActiveDays { get; set; } = new();
        public int ClubTarget { get; set; }
    }

    public class DailyMissionVM
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public int Progress { get; set; }
        public int Target { get; set; }
    }

    public class QuoteVM
    {
        public string Text { get; set; } = "";
        public string Author { get; set; } = "";
        public int Likes { get; set; }
    }

    public class MissionsSummaryVM
    {
        public List<DailyMissionVM> Missions { get; set; } = new();
        public int Marks { get; set; }
        public DateTime ResetsAt { get; set; }
        public QuoteVM Quote { get; set; } = new();
    }

    /// <summary>What an activity changed, so callers can build their reward response.</summary>
    public record ActivityResult(bool StreakIncreased, int Streak);
}
