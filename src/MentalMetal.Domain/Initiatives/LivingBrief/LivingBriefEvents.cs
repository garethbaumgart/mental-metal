using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Initiatives.LivingBrief;

public sealed record LivingBriefSummaryUpdated(Guid InitiativeId, Guid UserId, BriefSource Source, int BriefVersion) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record LivingBriefDecisionLogged(Guid InitiativeId, Guid UserId, Guid DecisionId, BriefSource Source, int BriefVersion) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record LivingBriefRiskRaised(Guid InitiativeId, Guid UserId, Guid RiskId, RiskSeverity Severity, BriefSource Source, int BriefVersion) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record LivingBriefRiskResolved(Guid InitiativeId, Guid UserId, Guid RiskId, int BriefVersion) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record LivingBriefRequirementsSnapshot(Guid InitiativeId, Guid UserId, Guid SnapshotId, BriefSource Source, int BriefVersion) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record LivingBriefDesignDirectionSnapshot(Guid InitiativeId, Guid UserId, Guid SnapshotId, BriefSource Source, int BriefVersion) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
