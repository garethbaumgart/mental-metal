using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Commitments;

public sealed record CommitmentCreated(Guid CommitmentId, Guid UserId, CommitmentDirection Direction, Guid PersonId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CommitmentCompleted(Guid CommitmentId, string? Notes) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CommitmentCancelled(Guid CommitmentId, string? Reason) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CommitmentDismissed(Guid CommitmentId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CommitmentReopened(Guid CommitmentId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CommitmentDueDateChanged(Guid CommitmentId, DateOnly? NewDueDate) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CommitmentDescriptionUpdated(Guid CommitmentId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CommitmentLinkedToInitiative(Guid CommitmentId, Guid InitiativeId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CommitmentBecameOverdue(Guid CommitmentId, DateOnly DueDate) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
