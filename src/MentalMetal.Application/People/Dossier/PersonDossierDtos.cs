using MentalMetal.Domain.Commitments;

namespace MentalMetal.Application.People.Dossier;

public sealed record PersonDossierResponse(
    Guid PersonId,
    string PersonName,
    string Synthesis,
    List<DossierCommitmentDto> OpenCommitments,
    List<TranscriptMentionDto> TranscriptMentions,
    List<UnresolvedMentionDto> UnresolvedMentions,
    DateTimeOffset GeneratedAt);

public sealed record DossierCommitmentDto(
    Guid Id,
    string Description,
    CommitmentDirection Direction,
    DateOnly? DueDate,
    bool IsOverdue,
    CommitmentConfidence Confidence);

public sealed record TranscriptMentionDto(
    Guid CaptureId,
    string? CaptureTitle,
    DateTimeOffset CapturedAt,
    string? ExtractionSummary,
    string? MentionContext);

public sealed record UnresolvedMentionDto(
    Guid CaptureId,
    string RawName,
    string? Context);
