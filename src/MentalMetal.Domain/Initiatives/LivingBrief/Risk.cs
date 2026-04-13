using System.Collections.Immutable;

namespace MentalMetal.Domain.Initiatives.LivingBrief;

public sealed record Risk
{
    public required Guid Id { get; init; }
    public required string Description { get; init; }
    public required RiskSeverity Severity { get; init; }
    public required RiskStatus Status { get; init; }
    public required BriefSource Source { get; init; }
    // ImmutableArray prevents callers from casting back to a mutable list and rewriting brief history.
    public ImmutableArray<Guid> SourceCaptureIds { get; init; } = [];
    public required DateTimeOffset RaisedAt { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
    public string? ResolutionNote { get; init; }
    public int SchemaVersion { get; init; } = 1;

    public Risk Resolve(DateTimeOffset resolvedAt, string? resolutionNote) => this with
    {
        Status = RiskStatus.Resolved,
        ResolvedAt = resolvedAt,
        ResolutionNote = resolutionNote
    };
}
