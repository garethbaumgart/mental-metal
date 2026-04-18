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

public sealed record InitiativeSummaryRefreshed(Guid InitiativeId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
