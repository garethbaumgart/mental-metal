using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Delegations;

public sealed class Delegation : AggregateRoot, IUserScoped
{
    public Guid UserId { get; private set; }
    public string Description { get; private set; } = null!;
    public Guid DelegatePersonId { get; private set; }
    public Guid? InitiativeId { get; private set; }
    public Guid? SourceCaptureId { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DelegationStatus Status { get; private set; }
    public Priority Priority { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset? LastFollowedUpAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Delegation() { } // EF Core

    public static Delegation Create(
        Guid userId,
        string description,
        Guid delegatePersonId,
        DateOnly? dueDate = null,
        Guid? initiativeId = null,
        Priority? priority = null,
        Guid? sourceCaptureId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description, nameof(description));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (delegatePersonId == Guid.Empty)
            throw new ArgumentException("DelegatePersonId is required.", nameof(delegatePersonId));

        var now = DateTimeOffset.UtcNow;

        var delegation = new Delegation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Description = description.Trim(),
            DelegatePersonId = delegatePersonId,
            InitiativeId = initiativeId,
            SourceCaptureId = sourceCaptureId,
            DueDate = dueDate,
            Status = DelegationStatus.Assigned,
            Priority = priority ?? Priority.Medium,
            CreatedAt = now,
            UpdatedAt = now
        };

        delegation.RaiseDomainEvent(new DelegationCreated(delegation.Id, userId, delegatePersonId));

        return delegation;
    }

    public void MarkInProgress()
    {
        if (Status != DelegationStatus.Assigned)
            throw new InvalidOperationException(
                $"Cannot start a delegation with status '{Status}'. Must be 'Assigned'.");

        Status = DelegationStatus.InProgress;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new DelegationStarted(Id));
    }

    public void MarkCompleted(string? notes = null)
    {
        if (Status == DelegationStatus.Completed)
            throw new InvalidOperationException(
                "Cannot complete a delegation that is already 'Completed'.");

        Status = DelegationStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(notes))
            Notes = string.IsNullOrWhiteSpace(Notes) ? notes.Trim() : $"{Notes}\n{notes.Trim()}";

        RaiseDomainEvent(new DelegationCompleted(Id, notes));
    }

    public void MarkBlocked(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));

        if (Status != DelegationStatus.Assigned && Status != DelegationStatus.InProgress)
            throw new InvalidOperationException(
                $"Cannot block a delegation with status '{Status}'. Must be 'Assigned' or 'InProgress'.");

        Status = DelegationStatus.Blocked;
        UpdatedAt = DateTimeOffset.UtcNow;

        Notes = string.IsNullOrWhiteSpace(Notes) ? reason.Trim() : $"{Notes}\n{reason.Trim()}";

        RaiseDomainEvent(new DelegationBlocked(Id, reason));
    }

    public void Unblock()
    {
        if (Status != DelegationStatus.Blocked)
            throw new InvalidOperationException(
                $"Cannot unblock a delegation with status '{Status}'. Must be 'Blocked'.");

        Status = DelegationStatus.InProgress;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new DelegationUnblocked(Id));
    }

    public void RecordFollowUp(string? notes = null)
    {
        LastFollowedUpAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(notes))
            Notes = string.IsNullOrWhiteSpace(Notes) ? notes.Trim() : $"{Notes}\n{notes.Trim()}";

        RaiseDomainEvent(new DelegationFollowedUp(Id, notes));
    }

    public void UpdateDueDate(DateOnly? newDate)
    {
        DueDate = newDate;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new DelegationDueDateChanged(Id, newDate));
    }

    public void Reprioritize(Priority newPriority)
    {
        Priority = newPriority;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new DelegationReprioritized(Id, newPriority));
    }

    public void Reassign(Guid newPersonId)
    {
        if (newPersonId == Guid.Empty)
            throw new ArgumentException("NewPersonId is required.", nameof(newPersonId));

        if (DelegatePersonId == newPersonId)
            return; // idempotent

        var oldPersonId = DelegatePersonId;
        DelegatePersonId = newPersonId;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new DelegationReassigned(Id, oldPersonId, newPersonId));
    }

    public void UpdateDescription(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description, nameof(description));

        Description = description.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
