using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;

namespace MentalMetal.Application.Captures;

public sealed record CreateCaptureRequest(string RawContent, CaptureType Type, CaptureSource? Source = null, string? Title = null);

public sealed record UpdateCaptureMetadataRequest(string? Title);

public sealed record AiExtractionResponse(
    string Summary,
    List<PersonMentionResponse> PeopleMentioned,
    List<ExtractedCommitmentResponse> Commitments,
    List<string> Decisions,
    List<string> Risks,
    List<InitiativeTagResponse> InitiativeTags,
    DateTimeOffset ExtractedAt,
    CaptureType? DetectedCaptureType)
{
    public static AiExtractionResponse? From(AiExtraction? extraction) =>
        extraction is null ? null : new(
            extraction.Summary,
            (extraction.PeopleMentioned ?? []).Select(p => new PersonMentionResponse(p.RawName, p.PersonId, p.Context)).ToList(),
            (extraction.Commitments ?? []).Select(c => new ExtractedCommitmentResponse(
                c.Description, c.Direction, c.PersonId, c.DueDate, c.Confidence, c.SpawnedCommitmentId)).ToList(),
            (extraction.Decisions ?? []).ToList(),
            (extraction.Risks ?? []).ToList(),
            (extraction.InitiativeTags ?? []).Select(t => new InitiativeTagResponse(t.RawName, t.InitiativeId, t.Context)).ToList(),
            extraction.ExtractedAt,
            extraction.DetectedCaptureType);
}

public sealed record PersonMentionResponse(string RawName, Guid? PersonId, string? Context);
public sealed record ExtractedCommitmentResponse(
    string Description, CommitmentDirection Direction, Guid? PersonId,
    DateTimeOffset? DueDate, CommitmentConfidence Confidence, Guid? SpawnedCommitmentId);
public sealed record InitiativeTagResponse(string RawName, Guid? InitiativeId, string? Context);

public sealed record CaptureResponse(
    Guid Id,
    Guid UserId,
    string RawContent,
    CaptureType CaptureType,
    CaptureSource? CaptureSource,
    ProcessingStatus ProcessingStatus,
    AiExtractionResponse? AiExtraction,
    string? FailureReason,
    List<Guid> LinkedPersonIds,
    List<Guid> LinkedInitiativeIds,
    List<Guid> SpawnedCommitmentIds,
    string? Title,
    DateTimeOffset CapturedAt,
    DateTimeOffset? ProcessedAt,
    DateTimeOffset UpdatedAt)
{
    public static CaptureResponse From(Capture capture) => new(
        capture.Id,
        capture.UserId,
        capture.RawContent,
        capture.CaptureType,
        capture.CaptureSource,
        capture.ProcessingStatus,
        AiExtractionResponse.From(capture.AiExtraction),
        capture.FailureReason,
        capture.LinkedPersonIds.ToList(),
        capture.LinkedInitiativeIds.ToList(),
        capture.SpawnedCommitmentIds.ToList(),
        capture.Title,
        capture.CapturedAt,
        capture.ProcessedAt,
        capture.UpdatedAt);
}
