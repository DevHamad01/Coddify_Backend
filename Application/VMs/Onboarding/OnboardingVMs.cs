using System.ComponentModel.DataAnnotations;

namespace Application.VMs.Onboarding
{
    /// <summary>Body of POST /api/onboarding (frontend onboarding.models.ts SaveOnboardingRequest).</summary>
    public class SaveOnboardingVM
    {
        /// <summary>job | university | freelancing | fun</summary>
        [Required, RegularExpression("^(job|university|freelancing|fun)$")]
        public string Goal { get; set; } = "";

        /// <summary>A course slug or "unsure".</summary>
        [Required, MaxLength(30)]
        public string Language { get; set; } = "";

        /// <summary>beginner | some | comfortable</summary>
        [Required, RegularExpression("^(beginner|some|comfortable)$")]
        public string Experience { get; set; } = "";

        [Range(5, 20)]
        public int DailyMinutes { get; set; }

        /// <summary>The course the onboarding recommended; the learner is enrolled in it.</summary>
        [Required, MaxLength(30)]
        public string RecommendedLanguage { get; set; } = "";
    }

    public class OnboardingResultVM
    {
        public bool OnboardingCompleted { get; set; } = true;
    }
}
