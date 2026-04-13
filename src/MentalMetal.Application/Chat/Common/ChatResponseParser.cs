using System.Text.Json;
using MentalMetal.Domain.ChatThreads;

namespace MentalMetal.Application.Chat.Common;

/// <summary>
/// Shared structured-envelope parser used by both the initiative- and global-scope chat
/// completion services. Tolerates code-fence wrappers and stray prose around the JSON.
/// </summary>
public static class ChatResponseParser
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public sealed record ParsedEnvelope(string AssistantText, IReadOnlyList<ParsedSourceReference> SourceReferences);
    public sealed record ParsedSourceReference(string EntityType, Guid EntityId, string? SnippetText, decimal? RelevanceScore);

    public static ParsedEnvelope? TryParse(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;

        var stripped = content.Trim();
        if (stripped.StartsWith("```"))
        {
            var firstNl = stripped.IndexOf('\n');
            if (firstNl > 0) stripped = stripped[(firstNl + 1)..];
            if (stripped.EndsWith("```")) stripped = stripped[..^3];
            stripped = stripped.Trim();
        }

        var firstBrace = stripped.IndexOf('{');
        var lastBrace = stripped.LastIndexOf('}');
        if (firstBrace < 0 || lastBrace <= firstBrace) return null;

        var json = stripped[firstBrace..(lastBrace + 1)];
        try
        {
            var raw = JsonSerializer.Deserialize<RawEnvelope>(json, JsonOpts);
            if (raw is null || string.IsNullOrWhiteSpace(raw.AssistantText)) return null;
            return new ParsedEnvelope(
                raw.AssistantText,
                (raw.SourceReferences ?? [])
                    .Select(r => new ParsedSourceReference(r.EntityType, r.EntityId, r.SnippetText, r.RelevanceScore))
                    .ToList());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Build a SourceReference, dropping any whose (type, id) pair isn't in the known set
    /// from the assembled context. Returns null on any validation failure.
    /// </summary>
    public static SourceReference? TryBuildReference(
        ParsedSourceReference raw,
        HashSet<(SourceReferenceEntityType Type, Guid Id)> known)
    {
        if (!Enum.TryParse<SourceReferenceEntityType>(raw.EntityType, ignoreCase: true, out var type))
            return null;
        if (raw.EntityId == Guid.Empty) return null;
        if (!known.Contains((type, raw.EntityId))) return null;

        try
        {
            return new SourceReference(type, raw.EntityId, raw.SnippetText, raw.RelevanceScore);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private sealed record RawEnvelope(string? AssistantText, List<RawRef>? SourceReferences);
    private sealed record RawRef(string EntityType, Guid EntityId, string? SnippetText, decimal? RelevanceScore);
}
