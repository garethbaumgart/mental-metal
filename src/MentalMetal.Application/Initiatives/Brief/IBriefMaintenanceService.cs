namespace MentalMetal.Application.Initiatives.Brief;

public interface IBriefMaintenanceService
{
    /// <summary>
    /// Refreshes the living brief for the given initiative by gathering linked
    /// confirmed captures, calling the AI provider, and persisting a
    /// <see cref="MentalMetal.Domain.Initiatives.LivingBrief.PendingBriefUpdate"/>.
    /// On AI provider errors / taste-limit, a Failed proposal is persisted instead.
    /// Returns the persisted PendingBriefUpdate id.
    /// </summary>
    Task<Guid> RefreshAsync(Guid userId, Guid initiativeId, CancellationToken cancellationToken);

    /// <summary>
    /// Enqueues a debounced refresh job for the given initiative.
    /// Coalesces concurrent triggers per (userId, initiativeId).
    /// </summary>
    void EnqueueRefresh(Guid userId, Guid initiativeId);
}
