namespace MentalMetal.Application.Common;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Discards any in-memory entity changes that have not yet been flushed (clears the change tracker).
    /// Used when a multi-step operation fails partway through and we need to record a failure record
    /// without also persisting the partial mutations from the failed operation.
    /// </summary>
    void DiscardPendingChanges();
}
