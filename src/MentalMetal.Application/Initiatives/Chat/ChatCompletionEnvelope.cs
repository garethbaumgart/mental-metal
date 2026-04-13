namespace MentalMetal.Application.Initiatives.Chat;

/// <summary>
/// Structured envelope the AI is asked to emit.
/// </summary>
internal sealed record ChatCompletionEnvelope(
    string AssistantText,
    List<EnvelopeSourceReference> SourceReferences);

internal sealed record EnvelopeSourceReference(
    string EntityType,
    Guid EntityId,
    string? SnippetText,
    decimal? RelevanceScore);
