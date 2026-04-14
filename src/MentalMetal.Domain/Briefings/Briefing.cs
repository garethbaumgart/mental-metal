using System.Text.Json;
using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Briefings;

/// <summary>
/// Lightweight read-model record of an AI-generated briefing (morning, weekly, 1:1 prep).
/// Briefings are immutable artefacts of a generation request - mutators are not provided.
/// User-scoped via <see cref="UserId"/>.
/// </summary>
public sealed class Briefing : AggregateRoot, IUserScoped
{
    public Guid UserId { get; private set; }
    public BriefingType Type { get; private set; }
    public string ScopeKey { get; private set; } = null!;
    public DateTimeOffset GeneratedAtUtc { get; private set; }
    public string MarkdownBody { get; private set; } = null!;
    public string PromptFactsJson { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public int InputTokens { get; private set; }
    public int OutputTokens { get; private set; }

    private Briefing() { } // EF Core

    public static Briefing Create(
        Guid userId,
        BriefingType type,
        string scopeKey,
        DateTimeOffset generatedAtUtc,
        string markdownBody,
        string promptFactsJson,
        string model,
        int inputTokens,
        int outputTokens)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        ArgumentException.ThrowIfNullOrWhiteSpace(scopeKey, nameof(scopeKey));
        ArgumentNullException.ThrowIfNull(markdownBody, nameof(markdownBody));
        ArgumentException.ThrowIfNullOrWhiteSpace(promptFactsJson, nameof(promptFactsJson));
        ArgumentException.ThrowIfNullOrWhiteSpace(model, nameof(model));

        // The column is jsonb - the database will reject malformed JSON late at flush
        // time. Validate eagerly so callers see the bug at the call site instead of
        // a DbUpdateException downstream.
        try
        {
            using var _ = JsonDocument.Parse(promptFactsJson);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException(
                "PromptFactsJson must be a valid JSON document.", nameof(promptFactsJson), ex);
        }

        if (scopeKey.Length > 128)
            throw new ArgumentException("ScopeKey must be 128 characters or fewer.", nameof(scopeKey));
        if (model.Length > 64)
            throw new ArgumentException("Model must be 64 characters or fewer.", nameof(model));
        if (inputTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(inputTokens), "InputTokens cannot be negative.");
        if (outputTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(outputTokens), "OutputTokens cannot be negative.");

        return new Briefing
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            ScopeKey = scopeKey,
            GeneratedAtUtc = generatedAtUtc,
            MarkdownBody = markdownBody,
            PromptFactsJson = promptFactsJson,
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
        };
    }
}
