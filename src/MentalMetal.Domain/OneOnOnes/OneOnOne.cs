using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.OneOnOnes;

public sealed class OneOnOne : AggregateRoot, IUserScoped
{
    private readonly List<ActionItem> _actionItems = [];
    private readonly List<FollowUp> _followUps = [];
    private readonly List<string> _topics = [];

    public Guid UserId { get; private set; }
    public Guid PersonId { get; private set; }
    public DateOnly OccurredAt { get; private set; }
    public string? Notes { get; private set; }
    public int? MoodRating { get; private set; }
    public IReadOnlyList<string> Topics => _topics;
    public IReadOnlyList<ActionItem> ActionItems => _actionItems;
    public IReadOnlyList<FollowUp> FollowUps => _followUps;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private OneOnOne() { } // EF Core

    public static OneOnOne Create(
        Guid userId,
        Guid personId,
        DateOnly occurredAt,
        string? notes = null,
        IEnumerable<string>? topics = null,
        int? moodRating = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        if (moodRating is not null)
            _ = OneOnOnes.MoodRating.Create(moodRating.Value); // validate

        var now = DateTimeOffset.UtcNow;
        var oneOnOne = new OneOnOne
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PersonId = personId,
            OccurredAt = occurredAt,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            MoodRating = moodRating,
            CreatedAt = now,
            UpdatedAt = now,
        };

        if (topics is not null)
        {
            foreach (var t in topics)
            {
                if (!string.IsNullOrWhiteSpace(t))
                    oneOnOne._topics.Add(t.Trim());
            }
        }

        oneOnOne.RaiseDomainEvent(new OneOnOneCreated(oneOnOne.Id, userId, personId, occurredAt));

        return oneOnOne;
    }

    public void Update(string? notes, IEnumerable<string>? topics, int? moodRating)
    {
        if (moodRating is not null)
            _ = OneOnOnes.MoodRating.Create(moodRating.Value); // validate

        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        MoodRating = moodRating;
        _topics.Clear();
        if (topics is not null)
        {
            foreach (var t in topics)
            {
                if (!string.IsNullOrWhiteSpace(t))
                    _topics.Add(t.Trim());
            }
        }
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new OneOnOneUpdated(Id));
    }

    public ActionItem AddActionItem(string description)
    {
        var item = ActionItem.Create(description);
        _actionItems.Add(item);
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ActionItemAdded(Id, item.Id));
        return item;
    }

    public void CompleteActionItem(Guid actionItemId)
    {
        var existing = _actionItems.FirstOrDefault(a => a.Id == actionItemId)
            ?? throw new ArgumentException($"Action item '{actionItemId}' not found.");

        if (existing.Completed)
            return;

        existing.MarkCompleted();
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ActionItemCompleted(Id, actionItemId));
    }

    public void RemoveActionItem(Guid actionItemId)
    {
        var removed = _actionItems.RemoveAll(a => a.Id == actionItemId);
        if (removed == 0)
            throw new ArgumentException($"Action item '{actionItemId}' not found.");

        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ActionItemRemoved(Id, actionItemId));
    }

    public FollowUp AddFollowUp(string description)
    {
        var fu = FollowUp.Create(description);
        _followUps.Add(fu);
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new FollowUpAdded(Id, fu.Id));
        return fu;
    }

    public void ResolveFollowUp(Guid followUpId)
    {
        var existing = _followUps.FirstOrDefault(f => f.Id == followUpId)
            ?? throw new ArgumentException($"Follow-up '{followUpId}' not found.");

        if (existing.Resolved)
            return;

        existing.MarkResolved();
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new FollowUpResolved(Id, followUpId));
    }
}
