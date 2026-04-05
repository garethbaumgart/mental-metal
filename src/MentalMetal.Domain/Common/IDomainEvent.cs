namespace MentalMetal.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
