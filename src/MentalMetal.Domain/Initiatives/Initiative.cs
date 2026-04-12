using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Initiatives;

public sealed class Initiative : AggregateRoot, IUserScoped
{
    private readonly List<Milestone> _milestones = [];
    private readonly List<Guid> _linkedPersonIds = [];

    public Guid UserId { get; private set; }
    public string Title { get; private set; } = null!;
    public InitiativeStatus Status { get; private set; }
    public IReadOnlyList<Milestone> Milestones => _milestones;
    public IReadOnlyList<Guid> LinkedPersonIds => _linkedPersonIds;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Initiative() { } // EF Core

    public static Initiative Create(Guid userId, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        var now = DateTimeOffset.UtcNow;

        var initiative = new Initiative
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title.Trim(),
            Status = InitiativeStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        initiative.RaiseDomainEvent(new InitiativeCreated(initiative.Id, userId, initiative.Title));

        return initiative;
    }

    public void UpdateTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        EnsureNotTerminal();

        Title = title.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new InitiativeTitleUpdated(Id));
    }

    public void ChangeStatus(InitiativeStatus newStatus)
    {
        if (Status == newStatus)
            return;

        EnsureNotTerminal();

        // Validate state machine: Active -> OnHold/Completed/Cancelled, OnHold -> Active
        var isValid = (Status, newStatus) switch
        {
            (InitiativeStatus.Active, InitiativeStatus.OnHold) => true,
            (InitiativeStatus.Active, InitiativeStatus.Completed) => true,
            (InitiativeStatus.Active, InitiativeStatus.Cancelled) => true,
            (InitiativeStatus.OnHold, InitiativeStatus.Active) => true,
            _ => false
        };

        if (!isValid)
            throw new ArgumentException($"Invalid status transition from '{Status}' to '{newStatus}'.");

        var oldStatus = Status;
        Status = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new InitiativeStatusChanged(Id, oldStatus, newStatus));
    }

    public void AddMilestone(string title, DateOnly targetDate, string? description = null)
    {
        EnsureNotTerminal();

        var milestone = Milestone.Create(title, targetDate, description);
        _milestones.Add(milestone);
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new MilestoneSet(Id, milestone.Id));
    }

    public void UpdateMilestone(Guid milestoneId, string title, DateOnly targetDate, string? description = null)
    {
        EnsureNotTerminal();

        var existing = _milestones.FirstOrDefault(m => m.Id == milestoneId)
            ?? throw new ArgumentException($"Milestone '{milestoneId}' not found.");

        _milestones.Remove(existing);
        _milestones.Add(existing.WithUpdates(title, targetDate, description));
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new MilestoneSet(Id, milestoneId));
    }

    public void RemoveMilestone(Guid milestoneId)
    {
        EnsureNotTerminal();

        var removed = _milestones.RemoveAll(m => m.Id == milestoneId);
        if (removed == 0)
            throw new ArgumentException($"Milestone '{milestoneId}' not found.");

        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new MilestoneRemoved(Id, milestoneId));
    }

    public void CompleteMilestone(Guid milestoneId)
    {
        EnsureNotTerminal();

        var existing = _milestones.FirstOrDefault(m => m.Id == milestoneId)
            ?? throw new ArgumentException($"Milestone '{milestoneId}' not found.");

        _milestones.Remove(existing);
        _milestones.Add(existing.Complete());
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new MilestoneCompleted(Id, milestoneId));
    }

    public void LinkPerson(Guid personId)
    {
        EnsureNotTerminal();

        if (_linkedPersonIds.Contains(personId))
            return;

        _linkedPersonIds.Add(personId);
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new PersonLinkedToInitiative(Id, personId));
    }

    public void UnlinkPerson(Guid personId)
    {
        EnsureNotTerminal();

        if (!_linkedPersonIds.Remove(personId))
            throw new ArgumentException($"Person '{personId}' is not linked to this initiative.");

        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new PersonUnlinkedFromInitiative(Id, personId));
    }

    private void EnsureNotTerminal()
    {
        if (Status is InitiativeStatus.Completed or InitiativeStatus.Cancelled)
            throw new ArgumentException($"Cannot modify an initiative in '{Status}' status.");
    }

}
