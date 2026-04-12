using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Commitments;

public sealed class Commitment : AggregateRoot, IUserScoped
{
    public Guid UserId { get; private set; }
    public string Description { get; private set; } = null!;
    public CommitmentDirection Direction { get; private set; }
    public Guid PersonId { get; private set; }
    public Guid? InitiativeId { get; private set; }
    public Guid? SourceCaptureId { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public CommitmentStatus Status { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsOverdue =>
        Status == CommitmentStatus.Open
        && DueDate is not null
        && DueDate < DateOnly.FromDateTime(DateTime.UtcNow);

    private Commitment() { } // EF Core

    public static Commitment Create(
        Guid userId,
        string description,
        CommitmentDirection direction,
        Guid personId,
        DateOnly? dueDate = null,
        Guid? initiativeId = null,
        Guid? sourceCaptureId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description, nameof(description));

        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        var now = DateTimeOffset.UtcNow;

        var commitment = new Commitment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Description = description.Trim(),
            Direction = direction,
            PersonId = personId,
            InitiativeId = initiativeId,
            SourceCaptureId = sourceCaptureId,
            DueDate = dueDate,
            Status = CommitmentStatus.Open,
            CreatedAt = now,
            UpdatedAt = now
        };

        commitment.RaiseDomainEvent(new CommitmentCreated(commitment.Id, userId, direction, personId));

        return commitment;
    }

    public void Complete(string? notes = null)
    {
        if (Status != CommitmentStatus.Open)
            throw new InvalidOperationException(
                $"Cannot complete a commitment with status '{Status}'. Must be 'Open'.");

        Status = CommitmentStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(notes))
            Notes = string.IsNullOrWhiteSpace(Notes) ? notes.Trim() : $"{Notes}\n{notes.Trim()}";

        RaiseDomainEvent(new CommitmentCompleted(Id, notes));
    }

    public void Cancel(string? reason = null)
    {
        if (Status != CommitmentStatus.Open)
            throw new InvalidOperationException(
                $"Cannot cancel a commitment with status '{Status}'. Must be 'Open'.");

        Status = CommitmentStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(reason))
            Notes = string.IsNullOrWhiteSpace(Notes) ? reason.Trim() : $"{Notes}\n{reason.Trim()}";

        RaiseDomainEvent(new CommitmentCancelled(Id, reason));
    }

    public void Reopen()
    {
        if (Status == CommitmentStatus.Open)
            throw new InvalidOperationException(
                "Cannot reopen a commitment that is already 'Open'.");

        Status = CommitmentStatus.Open;
        CompletedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CommitmentReopened(Id));
    }

    public void UpdateDueDate(DateOnly? newDate)
    {
        DueDate = newDate;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CommitmentDueDateChanged(Id, newDate));
    }

    public void UpdateDescription(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description, nameof(description));

        Description = description.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CommitmentDescriptionUpdated(Id));
    }

    public void LinkToInitiative(Guid initiativeId)
    {
        if (InitiativeId == initiativeId)
            return; // idempotent

        InitiativeId = initiativeId;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CommitmentLinkedToInitiative(Id, initiativeId));
    }

    public void MarkOverdue()
    {
        if (!IsOverdue)
            return; // guard: only raise event when actually overdue

        RaiseDomainEvent(new CommitmentBecameOverdue(Id, DueDate!.Value));
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
