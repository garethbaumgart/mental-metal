using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Initiatives;

public sealed class Initiative : AggregateRoot, IUserScoped
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = null!;
    public InitiativeStatus Status { get; private set; }
    public string? AutoSummary { get; private set; }
    public DateTimeOffset? LastSummaryRefreshedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Initiative() { } // EF Core

    public static Initiative Create(Guid userId, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

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

    /// <summary>
    /// Sets the AI-generated auto-summary. Called by the extraction/refresh pipeline.
    /// </summary>
    public void RefreshAutoSummary(string summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(summary, nameof(summary));

        AutoSummary = summary.Trim();
        LastSummaryRefreshedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new InitiativeSummaryRefreshed(Id));
    }

    private void EnsureNotTerminal()
    {
        if (Status is InitiativeStatus.Completed or InitiativeStatus.Cancelled)
            throw new ArgumentException($"Cannot modify an initiative in '{Status}' status.");
    }
}
