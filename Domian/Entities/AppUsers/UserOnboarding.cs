using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domian.Entities.AppUsers
{
    /// <summary>Answers from the onboarding questions, one row per user.</summary>
    public class UserOnboarding
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long UserId { get; set; }

        [MaxLength(30)]
        public string Goal { get; set; } = "";

        [MaxLength(30)]
        public string Language { get; set; } = "";

        [MaxLength(30)]
        public string Experience { get; set; } = "";

        public int DailyMinutes { get; set; }

        [MaxLength(30)]
        public string RecommendedLanguage { get; set; } = "";

        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }
}
