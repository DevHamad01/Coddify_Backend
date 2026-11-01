namespace Infrastructure.Services.Gamification
{
    /// <summary>
    /// Game rules shared by every gamification service. Values match the frontend mocks so the
    /// numbers learners saw in the demo stay the same with the real API.
    /// </summary>
    public static class GamificationRules
    {
        public const int LessonGems = 2;
        public const int ChestGems = 5;
        public const int SolveXp = 20;
        public const int XpMissionTarget = 50;
        public const int LeaderboardUnlockXp = 100;
        public const int StreakClubTarget = 3;
        public const int PromotionCutoff = 7;
        public const int BioLimit = 300;
        public const int ShowcaseLimit = 5;
        public const int MinDailyGoal = 2;
        public const int MaxDailyGoal = 12;

        /// <summary>Every level needs 150 XP more than the one before.</summary>
        public const int XpStep = 150;

        public static readonly (string Name, int From, int To)[] Tiers =
        {
            ("Kilo", 1, 6),
            ("Mega", 7, 16),
            ("Giga", 17, 28),
            ("Tera", 29, 40),
            ("Peta", 41, 50)
        };

        /// <summary>A computing word for every level (index 0 = level 1).</summary>
        public static readonly string[] LevelNames =
        {
            "Hello World",
            "Memory Blocks",
            "Underscore",
            "Clock Cycles",
            "Breakpoints",
            "Loading Stripes",
            "Stack Frames",
            "Heap Hero",
            "Bit Shifter",
            "Bytecode",
            "Pointer Path",
            "Cache Hit",
            "Thread Weaver",
            "Kernel",
            "Compiler",
            "Mega Mind",
            "Lambda",
            "Recursion",
            "Hash Map",
            "Binary Tree",
            "Graph Walker",
            "Dynamic Duo",
            "Greedy Choice",
            "Big O",
            "Refactor",
            "Unit Tested",
            "Code Review",
            "Giga Brain",
            "Microservice",
            "Pipeline",
            "Container",
            "Orchestrator",
            "Load Balancer",
            "Sharding",
            "Consensus",
            "Event Loop",
            "Async Await",
            "Garbage Collector",
            "Type System",
            "Tera Titan",
            "Quantum Bit",
            "Neural Net",
            "Transformer",
            "Compiler Wizard",
            "Open Source",
            "Architect",
            "Mentor",
            "Legend",
            "Hall of Fame",
            "Peta Master"
        };

        /// <summary>Item equipped in each slot until the learner picks another.</summary>
        public static readonly IReadOnlyDictionary<string, string> DefaultEquipped = new Dictionary<string, string>
        {
            ["theme"] = "theme-coddify",
            ["tutor"] = "tutor-bit",
            ["title"] = "bit-apprentice",
            ["frame"] = "frame-none",
            ["name-effect"] = "name-plain",
            ["streak-flame"] = "flame-classic"
        };

        public static string TierFor(int level) =>
            Tiers.FirstOrDefault(tier => level >= tier.From && level <= tier.To).Name ?? Tiers[^1].Name;

        /// <summary>Total XP needed to reach <paramref name="level"/> (level 1 needs 0).</summary>
        public static int XpToReach(int level) => XpStep * (level - 1) * level / 2;

        public static (int Level, string TierName, int XpIntoLevel, int XpForLevel) LevelFor(int totalXp)
        {
            var level = 1;
            var remaining = totalXp;
            while (remaining >= XpStep * level)
            {
                remaining -= XpStep * level;
                level++;
            }
            return (level, TierFor(level), remaining, XpStep * level);
        }
    }
}
