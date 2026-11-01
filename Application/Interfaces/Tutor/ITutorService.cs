using Application.VMs;
using Application.VMs.Tutor;

namespace Application.Interfaces.Tutor
{
    /// <summary>The AI tutor: hints and explanations about the current lesson or problem, never full solutions.</summary>
    public interface ITutorService
    {
        Task<ResponseVM> AskAsync(long userId, TutorRequestVM request, CancellationToken cancellationToken = default);
    }
}
