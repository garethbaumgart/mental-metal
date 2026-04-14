using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Observations;

public sealed class Observation : AggregateRoot, IUserScoped
{
    public Guid UserId { get; private set; }
    public Guid PersonId { get; private set; }
    public string Description { get; private set; } = null!;
    public ObservationTag Tag { get; private set; }
    public DateOnly OccurredAt { get; private set; }
    public Guid? SourceCaptureId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Observation() { } // EF Core

    public static Observation Create(
        Guid userId,
        Guid personId,
        string description,
        ObservationTag tag,
        DateOnly? occurredAt = null,
        Guid? sourceCaptureId = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        ArgumentException.ThrowIfNullOrWhiteSpace(description, nameof(description));

        var now = DateTimeOffset.UtcNow;
        var observation = new Observation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PersonId = personId,
            Description = description.Trim(),
            Tag = tag,
            OccurredAt = occurredAt ?? DateOnly.FromDateTime(DateTime.UtcNow),
            SourceCaptureId = sourceCaptureId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        observation.RaiseDomainEvent(new ObservationCreated(observation.Id, userId, personId, tag));
        return observation;
    }

    public void Update(string description, ObservationTag tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description, nameof(description));

        Description = description.Trim();
        Tag = tag;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ObservationUpdated(Id));
    }

    public void MarkDeleted()
    {
        RaiseDomainEvent(new ObservationDeleted(Id));
    }
}
