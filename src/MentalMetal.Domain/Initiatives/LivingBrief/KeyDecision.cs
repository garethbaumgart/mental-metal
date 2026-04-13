namespace MentalMetal.Domain.Initiatives.LivingBrief;

public sealed record KeyDecision
{
    public required Guid Id { get; init; }
    public required string Description { get; init; }
    public string? Rationale { get; init; }
    public required BriefSource Source { get; init; }
    public IReadOnlyList<Guid> SourceCaptureIds { get; init; } = [];
    public required DateTimeOffset LoggedAt { get; init; }
    public int SchemaVersion { get; init; } = 1;
}
