using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Nudges;

public sealed class Nudge : AggregateRoot, IUserScoped
{
    public const int MaxTitleLength = 200;
    public const int MaxNotesLength = 2000;

    public Guid UserId { get; private set; }
    public string Title { get; private set; } = null!;
    public NudgeCadence Cadence { get; private set; } = null!;
    public DateOnly StartDate { get; private set; }
    public DateOnly? NextDueDate { get; private set; }
    public DateTimeOffset? LastNudgedAt { get; private set; }
    public Guid? PersonId { get; private set; }
    public Guid? InitiativeId { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private Nudge() { } // EF Core

    public static Nudge Create(
        Guid userId,
        string title,
        NudgeCadence cadence,
        DateOnly today,
        DateOnly? startDate = null,
        Guid? personId = null,
        Guid? initiativeId = null,
        string? notes = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        ValidateTitle(title);
        ValidateNotes(notes);
        ArgumentNullException.ThrowIfNull(cadence);

        var effectiveStart = startDate ?? today;
        var now = DateTimeOffset.UtcNow;

        var nudge = new Nudge
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title.Trim(),
            Cadence = cadence,
            StartDate = effectiveStart,
            NextDueDate = cadence.CalculateFirst(effectiveStart),
            LastNudgedAt = null,
            PersonId = personId,
            InitiativeId = initiativeId,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        nudge.RaiseDomainEvent(new NudgeCreated(nudge.Id, userId));
        return nudge;
    }

    public void UpdateDetails(string title, string? notes, Guid? personId, Guid? initiativeId)
    {
        ValidateTitle(title);
        ValidateNotes(notes);

        var changed = false;
        var trimmed = title.Trim();
        if (Title != trimmed) { Title = trimmed; changed = true; }

        var newNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (Notes != newNotes) { Notes = newNotes; changed = true; }

        if (PersonId != personId) { PersonId = personId; changed = true; }
        if (InitiativeId != initiativeId) { InitiativeId = initiativeId; changed = true; }

        if (changed)
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow;
            RaiseDomainEvent(new NudgeUpdated(Id));
        }
    }

    public void UpdateCadence(NudgeCadence newCadence, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(newCadence);

        Cadence = newCadence;
        if (IsActive)
            NextDueDate = newCadence.CalculateFirst(today);

        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new NudgeCadenceChanged(Id, newCadence.Type));
    }

    public void MarkNudged(DateOnly today)
    {
        if (!IsActive)
            throw new DomainException("Cannot mark a paused nudge.", "nudge.notActive");

        LastNudgedAt = DateTimeOffset.UtcNow;
        NextDueDate = Cadence.CalculateNext(today);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new NudgeNudged(Id, NextDueDate.Value));
    }

    public void Pause()
    {
        if (!IsActive)
            throw new DomainException("Nudge is already paused.", "nudge.alreadyPaused");

        IsActive = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new NudgePaused(Id));
    }

    public void Resume(DateOnly today)
    {
        if (IsActive)
            throw new DomainException("Nudge is already active.", "nudge.alreadyActive");

        IsActive = true;
        NextDueDate = Cadence.CalculateFirst(today);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new NudgeResumed(Id, NextDueDate.Value));
    }

    public void MarkDeleted() => RaiseDomainEvent(new NudgeDeleted(Id));

    private static void ValidateTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        if (title.Trim().Length > MaxTitleLength)
            throw new ArgumentException($"Title must be {MaxTitleLength} characters or fewer.", nameof(title));
    }

    private static void ValidateNotes(string? notes)
    {
        if (notes is not null && notes.Length > MaxNotesLength)
            throw new ArgumentException($"Notes must be {MaxNotesLength} characters or fewer.", nameof(notes));
    }
}
