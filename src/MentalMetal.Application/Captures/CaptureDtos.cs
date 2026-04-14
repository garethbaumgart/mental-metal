using MentalMetal.Domain.Captures;

namespace MentalMetal.Application.Captures;

public sealed record CreateCaptureRequest(string RawContent, CaptureType Type, string? Title = null, string? Source = null);

public sealed record UpdateCaptureMetadataRequest(string? Title, string? Source);

public sealed record LinkPersonRequest(Guid PersonId);

public sealed record LinkInitiativeRequest(Guid InitiativeId);

public sealed record AiExtractionResponse(
    string Summary,
    List<ExtractedCommitmentResponse> Commitments,
    List<ExtractedDelegationResponse> Delegations,
    List<ExtractedObservationResponse> Observations,
    List<string> Decisions,
    List<string> RisksIdentified,
    List<string> SuggestedPersonLinks,
    List<string> SuggestedInitiativeLinks,
    decimal ConfidenceScore)
{
    public static AiExtractionResponse? From(AiExtraction? extraction) =>
        extraction is null ? null : new(
            extraction.Summary,
            extraction.Commitments.Select(c => new ExtractedCommitmentResponse(c.Description, c.Direction.ToString(), c.PersonHint, c.DueDate)).ToList(),
            extraction.Delegations.Select(d => new ExtractedDelegationResponse(d.Description, d.PersonHint, d.DueDate)).ToList(),
            extraction.Observations.Select(o => new ExtractedObservationResponse(o.Description, o.PersonHint, o.Tag)).ToList(),
            extraction.Decisions.ToList(),
            extraction.RisksIdentified.ToList(),
            extraction.SuggestedPersonLinks.ToList(),
            extraction.SuggestedInitiativeLinks.ToList(),
            extraction.ConfidenceScore);
}

public sealed record ExtractedCommitmentResponse(string Description, string Direction, string? PersonHint, string? DueDate);
public sealed record ExtractedDelegationResponse(string Description, string? PersonHint, string? DueDate);
public sealed record ExtractedObservationResponse(string Description, string? PersonHint, string? Tag);

public sealed record ConfirmExtractionResponse(CaptureResponse Capture, IReadOnlyList<string> Warnings);

public sealed record CaptureResponse(
    Guid Id,
    Guid UserId,
    string RawContent,
    CaptureType CaptureType,
    ProcessingStatus ProcessingStatus,
    ExtractionStatus ExtractionStatus,
    AiExtractionResponse? AiExtraction,
    string? FailureReason,
    List<Guid> LinkedPersonIds,
    List<Guid> LinkedInitiativeIds,
    List<Guid> SpawnedCommitmentIds,
    List<Guid> SpawnedDelegationIds,
    List<Guid> SpawnedObservationIds,
    string? Title,
    DateTimeOffset CapturedAt,
    DateTimeOffset? ProcessedAt,
    string? Source,
    DateTimeOffset UpdatedAt,
    bool Triaged,
    DateTimeOffset? TriagedAtUtc,
    bool ExtractionResolved)
{
    public static CaptureResponse From(Capture capture) => new(
        capture.Id,
        capture.UserId,
        capture.RawContent,
        capture.CaptureType,
        capture.ProcessingStatus,
        capture.ExtractionStatus,
        AiExtractionResponse.From(capture.AiExtraction),
        capture.FailureReason,
        capture.LinkedPersonIds.ToList(),
        capture.LinkedInitiativeIds.ToList(),
        capture.SpawnedCommitmentIds.ToList(),
        capture.SpawnedDelegationIds.ToList(),
        capture.SpawnedObservationIds.ToList(),
        capture.Title,
        capture.CapturedAt,
        capture.ProcessedAt,
        capture.Source,
        capture.UpdatedAt,
        capture.Triaged,
        capture.TriagedAtUtc,
        capture.ExtractionResolved);
}
