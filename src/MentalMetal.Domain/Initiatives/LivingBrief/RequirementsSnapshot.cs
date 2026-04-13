namespace MentalMetal.Domain.Initiatives.LivingBrief;

public sealed record RequirementsSnapshot
{
    public required Guid Id { get; init; }
    public required string Content { get; init; }
    public required BriefSource Source { get; init; }
    public IReadOnlyList<Guid> SourceCaptureIds { get; init; } = [];
    public required DateTimeOffset CapturedAt { get; init; }
    public int SchemaVersion { get; init; } = 1;
}

public sealed record DesignDirectionSnapshot
{
    public required Guid Id { get; init; }
    public required string Content { get; init; }
    public required BriefSource Source { get; init; }
    public IReadOnlyList<Guid> SourceCaptureIds { get; init; } = [];
    public required DateTimeOffset CapturedAt { get; init; }
    public int SchemaVersion { get; init; } = 1;
}
