using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.ChatThreads;

/// <summary>
/// Structured citation attached to assistant messages. The front-end resolves the link target
/// based on <see cref="EntityType"/>.
/// </summary>
public sealed class SourceReference : ValueObject
{
    public SourceReferenceEntityType EntityType { get; }
    public Guid EntityId { get; }
    public string? SnippetText { get; }
    public decimal? RelevanceScore { get; }

    public SourceReference(
        SourceReferenceEntityType entityType,
        Guid entityId,
        string? snippetText = null,
        decimal? relevanceScore = null)
    {
        if (!Enum.IsDefined(entityType))
            throw new ArgumentException($"Unknown SourceReference EntityType '{entityType}'.", nameof(entityType));

        if (entityId == Guid.Empty)
            throw new ArgumentException("EntityId is required.", nameof(entityId));

        if (relevanceScore is not null && (relevanceScore < 0m || relevanceScore > 1m))
            throw new ArgumentException("RelevanceScore must be between 0.0 and 1.0.", nameof(relevanceScore));

        EntityType = entityType;
        EntityId = entityId;
        SnippetText = string.IsNullOrWhiteSpace(snippetText) ? null : snippetText.Trim();
        RelevanceScore = relevanceScore;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return EntityType;
        yield return EntityId;
        yield return SnippetText;
        yield return RelevanceScore;
    }
}
