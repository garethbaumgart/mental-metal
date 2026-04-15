using MentalMetal.Domain.Commitments;

namespace MentalMetal.Application.Commitments;

public sealed record CreateCommitmentRequest(
    string Description,
    CommitmentDirection? Direction,
    Guid PersonId,
    DateOnly? DueDate = null,
    Guid? InitiativeId = null,
    Guid? SourceCaptureId = null,
    string? Notes = null);

public sealed record UpdateCommitmentRequest(string Description, string? Notes);

public sealed record CompleteCommitmentRequest(string? Notes = null);

public sealed record CancelCommitmentRequest(string? Reason = null);

public sealed record UpdateDueDateRequest(DateOnly? DueDate);

public sealed record LinkCommitmentToInitiativeRequest(Guid InitiativeId);

public sealed record CommitmentResponse(
    Guid Id,
    Guid UserId,
    string Description,
    CommitmentDirection Direction,
    Guid PersonId,
    Guid? InitiativeId,
    Guid? SourceCaptureId,
    DateOnly? DueDate,
    CommitmentStatus Status,
    DateTimeOffset? CompletedAt,
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
        commitment.DueDate,
        commitment.Status,
        commitment.CompletedAt,
        commitment.Notes,
        commitment.IsOverdue,
        commitment.CreatedAt,
        commitment.UpdatedAt);
}
