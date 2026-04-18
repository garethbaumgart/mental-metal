using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.People;

public sealed record PersonCreated(Guid PersonId, Guid UserId, string Name, PersonType Type) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record PersonProfileUpdated(Guid PersonId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record PersonTypeChanged(Guid PersonId, PersonType OldType, PersonType NewType) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record PersonAliasesUpdated(Guid PersonId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record PersonArchived(Guid PersonId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
