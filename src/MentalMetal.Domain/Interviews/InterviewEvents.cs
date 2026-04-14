using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Interviews;

public sealed record InterviewCreated(Guid InterviewId, Guid UserId, Guid CandidatePersonId, string RoleTitle) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record InterviewUpdated(Guid InterviewId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record InterviewStageChanged(Guid InterviewId, InterviewStage FromStage, InterviewStage ToStage) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record InterviewDecisionRecorded(Guid InterviewId, InterviewDecision Decision) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record InterviewScorecardAdded(Guid InterviewId, Guid ScorecardId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record InterviewScorecardUpdated(Guid InterviewId, Guid ScorecardId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record InterviewScorecardRemoved(Guid InterviewId, Guid ScorecardId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record InterviewTranscriptSet(Guid InterviewId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record InterviewAnalysisGenerated(Guid InterviewId, string Model) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record InterviewDeleted(Guid InterviewId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
