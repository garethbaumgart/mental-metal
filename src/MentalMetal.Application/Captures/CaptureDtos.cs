using MentalMetal.Domain.Captures;

namespace MentalMetal.Application.Captures;

public sealed record CreateCaptureRequest(string RawContent, CaptureType Type, string? Title = null, string? Source = null);

public sealed record UpdateCaptureMetadataRequest(string? Title, string? Source);

public sealed record LinkPersonRequest(Guid PersonId);

public sealed record LinkInitiativeRequest(Guid InitiativeId);

public sealed record CaptureResponse(
    Guid Id,
    Guid UserId,
    string RawContent,
    CaptureType CaptureType,
    ProcessingStatus ProcessingStatus,
    string? AiExtraction,
    List<Guid> LinkedPersonIds,
    List<Guid> LinkedInitiativeIds,
    List<Guid> SpawnedCommitmentIds,
    List<Guid> SpawnedDelegationIds,
    List<Guid> SpawnedObservationIds,
    string? Title,
    DateTimeOffset CapturedAt,
    DateTimeOffset? ProcessedAt,
    string? Source,
    DateTimeOffset UpdatedAt)
{
    public static CaptureResponse From(Capture capture) => new(
        capture.Id,
        capture.UserId,
        capture.RawContent,
        capture.CaptureType,
        capture.ProcessingStatus,
        capture.AiExtraction,
        capture.LinkedPersonIds.ToList(),
        capture.LinkedInitiativeIds.ToList(),
        capture.SpawnedCommitmentIds.ToList(),
        capture.SpawnedDelegationIds.ToList(),
        capture.SpawnedObservationIds.ToList(),
        capture.Title,
        capture.CapturedAt,
        capture.ProcessedAt,
        capture.Source,
        capture.UpdatedAt);
}
