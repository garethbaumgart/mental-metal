using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Initiatives.LivingBrief;

public sealed class PendingBriefUpdate : AggregateRoot, IUserScoped
{
    public Guid UserId { get; private set; }
    public Guid InitiativeId { get; private set; }
    public BriefUpdateProposal Proposal { get; private set; } = null!;
    public PendingBriefUpdateStatus Status { get; private set; }
    public int BriefVersionAtProposal { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private PendingBriefUpdate() { } // EF Core

    public static PendingBriefUpdate Create(
        Guid userId,
        Guid initiativeId,
        BriefUpdateProposal proposal,
        int briefVersionAtProposal)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (initiativeId == Guid.Empty)
            throw new ArgumentException("InitiativeId is required.", nameof(initiativeId));
        ArgumentNullException.ThrowIfNull(proposal);

        var now = DateTimeOffset.UtcNow;
        var update = new PendingBriefUpdate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InitiativeId = initiativeId,
            Proposal = proposal,
            Status = PendingBriefUpdateStatus.Pending,
            BriefVersionAtProposal = briefVersionAtProposal,
            CreatedAt = now,
            UpdatedAt = now
        };

        update.RaiseDomainEvent(new LivingBriefUpdateProposed(update.Id, userId, initiativeId));
        return update;
    }

    public static PendingBriefUpdate CreateFailed(
        Guid userId,
        Guid initiativeId,
        int briefVersionAtProposal,
        string reason)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (initiativeId == Guid.Empty)
            throw new ArgumentException("InitiativeId is required.", nameof(initiativeId));

        var now = DateTimeOffset.UtcNow;
        return new PendingBriefUpdate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InitiativeId = initiativeId,
            Proposal = new BriefUpdateProposal(),
            Status = PendingBriefUpdateStatus.Failed,
            BriefVersionAtProposal = briefVersionAtProposal,
            FailureReason = reason,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkFailed(string reason)
    {
        EnsureActive();
        Status = PendingBriefUpdateStatus.Failed;
        FailureReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Edit(BriefUpdateProposal newProposal)
    {
        ArgumentNullException.ThrowIfNull(newProposal);
        EnsureActive();
        Proposal = newProposal;
        Status = PendingBriefUpdateStatus.Edited;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkApplied()
    {
        EnsureActive();
        Status = PendingBriefUpdateStatus.Applied;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new LivingBriefUpdateApplied(Id, UserId, InitiativeId));
    }

    public void Reject(string? reason)
    {
        EnsureActive();
        Status = PendingBriefUpdateStatus.Rejected;
        FailureReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new LivingBriefUpdateRejected(Id, UserId, InitiativeId));
    }

    private void EnsureActive()
    {
        if (Status is PendingBriefUpdateStatus.Applied
                   or PendingBriefUpdateStatus.Rejected
                   or PendingBriefUpdateStatus.Failed)
            throw new InvalidOperationException(
                $"Cannot transition a pending update in terminal status '{Status}'.");
    }
}

public sealed record LivingBriefUpdateProposed(Guid PendingUpdateId, Guid UserId, Guid InitiativeId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record LivingBriefUpdateApplied(Guid PendingUpdateId, Guid UserId, Guid InitiativeId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record LivingBriefUpdateRejected(Guid PendingUpdateId, Guid UserId, Guid InitiativeId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
