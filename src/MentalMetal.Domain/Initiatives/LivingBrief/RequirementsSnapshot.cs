using System.Collections.Immutable;

namespace MentalMetal.Domain.Initiatives.LivingBrief;

public sealed record RequirementsSnapshot
{
    public required Guid Id { get; init; }
    public required string Content { get; init; }
    public required BriefSource Source { get; init; }
    // ImmutableArray prevents callers from casting back to a mutable list and rewriting snapshot history.
    public ImmutableArray<Guid> SourceCaptureIds { get; init; } = [];
    public required DateTimeOffset CapturedAt { get; init; }
    public int SchemaVersion { get; init; } = 1;
}

public sealed record DesignDirectionSnapshot
{
    public required Guid Id { get; init; }
    public required string Content { get; init; }
    public required BriefSource Source { get; init; }
    // ImmutableArray prevents callers from casting back to a mutable list and rewriting snapshot history.
    public ImmutableArray<Guid> SourceCaptureIds { get; init; } = [];
    public required DateTimeOffset CapturedAt { get; init; }
    public int SchemaVersion { get; init; } = 1;
}
