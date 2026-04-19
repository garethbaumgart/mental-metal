using MentalMetal.Domain.Commitments;

namespace MentalMetal.Application.Commitments;

public sealed record CompleteCommitmentRequest(string? Notes = null);

public sealed record UpdateCommitmentRequest(
    string? Description = null,
    CommitmentDirection? Direction = null,
    DateOnly? DueDate = null,
    bool ClearDueDate = false,
    string? Notes = null,
    bool ClearNotes = false);

public sealed record CommitmentResponse(
    Guid Id,
    Guid UserId,
    string Description,
    CommitmentDirection Direction,
    Guid PersonId,
    Guid? InitiativeId,
    Guid? SourceCaptureId,
    CommitmentConfidence Confidence,
    DateOnly? DueDate,
    CommitmentStatus Status,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? DismissedAt,
    string? Notes,
    bool IsOverdue,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static CommitmentResponse From(Commitment commitment) => new(
        commitment.Id,
        commitment.UserId,
        commitment.Description,
        commitment.Direction,
        commitment.PersonId,
        commitment.InitiativeId,
        commitment.SourceCaptureId,
        commitment.Confidence,
        commitment.DueDate,
        commitment.Status,
        commitment.CompletedAt,
        commitment.DismissedAt,
        commitment.Notes,
        commitment.IsOverdue,
        commitment.CreatedAt,
        commitment.UpdatedAt);
}
