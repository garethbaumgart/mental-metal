using MentalMetal.Domain.Observations;

namespace MentalMetal.Application.Observations;

public sealed record CreateObservationRequest(
    Guid PersonId,
    string Description,
    ObservationTag Tag,
    DateOnly? OccurredAt = null,
    Guid? SourceCaptureId = null);

public sealed record UpdateObservationRequest(string Description, ObservationTag Tag);

public sealed record ObservationResponse(
    Guid Id,
    Guid UserId,
    Guid PersonId,
    string Description,
    ObservationTag Tag,
    DateOnly OccurredAt,
    Guid? SourceCaptureId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static ObservationResponse From(Observation o) => new(
        o.Id, o.UserId, o.PersonId, o.Description, o.Tag, o.OccurredAt,
        o.SourceCaptureId, o.CreatedAt, o.UpdatedAt);
}
