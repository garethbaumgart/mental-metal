using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Delegations;

public sealed record DelegationCreated(Guid DelegationId, Guid UserId, Guid DelegatePersonId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record DelegationStarted(Guid DelegationId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record DelegationCompleted(Guid DelegationId, string? Notes) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record DelegationBlocked(Guid DelegationId, string Reason) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record DelegationUnblocked(Guid DelegationId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record DelegationFollowedUp(Guid DelegationId, string? Notes) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record DelegationDueDateChanged(Guid DelegationId, DateOnly? NewDueDate) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record DelegationReprioritized(Guid DelegationId, Priority NewPriority) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record DelegationReassigned(Guid DelegationId, Guid OldPersonId, Guid NewPersonId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
