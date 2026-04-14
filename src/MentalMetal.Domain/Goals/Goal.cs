using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Goals;

public sealed class Goal : AggregateRoot, IUserScoped
{
    private readonly List<GoalCheckIn> _checkIns = [];

    public Guid UserId { get; private set; }
    public Guid PersonId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public GoalType Type { get; private set; }
    public GoalStatus Status { get; private set; }
    public DateOnly? TargetDate { get; private set; }
    public string? DeferralReason { get; private set; }
    public DateTimeOffset? AchievedAt { get; private set; }
    public IReadOnlyList<GoalCheckIn> CheckIns => _checkIns;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Goal() { } // EF Core

    public static Goal Create(
        Guid userId,
        Guid personId,
        string title,
        GoalType type,
        string? description = null,
        DateOnly? targetDate = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        var now = DateTimeOffset.UtcNow;
        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PersonId = personId,
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Type = type,
            Status = GoalStatus.Active,
            TargetDate = targetDate,
            CreatedAt = now,
            UpdatedAt = now,
        };

        goal.RaiseDomainEvent(new GoalCreated(goal.Id, userId, personId, type));
        return goal;
    }

    public void Update(string title, string? description, DateOnly? targetDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        TargetDate = targetDate;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new GoalUpdated(Id));
    }

    public void Achieve()
    {
        if (Status != GoalStatus.Active)
            throw new InvalidOperationException($"Cannot achieve a goal with status '{Status}'. Must be 'Active'.");

        Status = GoalStatus.Achieved;
        AchievedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new GoalAchieved(Id));
    }

    public void Miss()
    {
        if (Status != GoalStatus.Active)
            throw new InvalidOperationException($"Cannot miss a goal with status '{Status}'. Must be 'Active'.");

        Status = GoalStatus.Missed;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new GoalMissed(Id));
    }

    public void Defer(string? reason)
    {
        if (Status != GoalStatus.Active)
            throw new InvalidOperationException($"Cannot defer a goal with status '{Status}'. Must be 'Active'.");

        Status = GoalStatus.Deferred;
        DeferralReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new GoalDeferred(Id, DeferralReason));
    }

    public void Reactivate()
    {
        if (Status == GoalStatus.Active)
            throw new InvalidOperationException("Goal is already Active.");

        Status = GoalStatus.Active;
        AchievedAt = null;
        DeferralReason = null;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new GoalReactivated(Id));
    }

    public GoalCheckIn RecordCheckIn(string note, int? progress)
    {
        var checkIn = GoalCheckIn.Create(note, progress);
        _checkIns.Add(checkIn);
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new GoalCheckInRecorded(Id, checkIn.Id, progress));
        return checkIn;
    }
}
