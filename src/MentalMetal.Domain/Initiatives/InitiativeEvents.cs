using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Initiatives;

public sealed record InitiativeCreated(Guid InitiativeId, Guid UserId, string Title) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record InitiativeTitleUpdated(Guid InitiativeId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record InitiativeStatusChanged(Guid InitiativeId, InitiativeStatus OldStatus, InitiativeStatus NewStatus) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record MilestoneSet(Guid InitiativeId, Guid MilestoneId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record MilestoneRemoved(Guid InitiativeId, Guid MilestoneId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record MilestoneCompleted(Guid InitiativeId, Guid MilestoneId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record PersonLinkedToInitiative(Guid InitiativeId, Guid PersonId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record PersonUnlinkedFromInitiative(Guid InitiativeId, Guid PersonId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
