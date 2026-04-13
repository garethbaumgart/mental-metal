using System.Text;
using System.Text.Json;
using MentalMetal.Application.Common.Ai;

namespace MentalMetal.Application.Chat.Global;

/// <summary>
/// Single AI call that maps a user message onto the same <see cref="ChatIntent"/> set the
/// rule layer uses, but with looser semantic matching. Returns Generic on any parse / provider
/// failure so the orchestrator can still build a baseline payload.
/// </summary>
public sealed class AiIntentClassifier(IAiCompletionService ai) : IIntentClassifier
{
    public const int MaxTokens = 200;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public async Task<IntentSet> ClassifyAsync(Guid userId, string userMessage, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return IntentSet.Generic;

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = userMessage;

        try
        {
            var result = await ai.CompleteAsync(
                new AiCompletionRequest(systemPrompt, userPrompt, MaxTokens, Temperature: 0.0f),
                cancellationToken);

            return ParseEnvelope(result.Content) ?? IntentSet.Generic;
        }
        catch (AiProviderException)
        {
            return IntentSet.Generic;
        }
        catch (TasteLimitExceededException)
        {
            // Don't surface — the main completion call will surface it again. Just degrade.
            return IntentSet.Generic;
        }
    }

    private static string BuildSystemPrompt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("You classify a single user question into one or more buckets.");
        sb.AppendLine("Buckets: MyDay, MyWeek, OverdueWork, PersonLens, InitiativeStatus, CaptureSearch, Generic.");
        sb.AppendLine("- MyDay: today's commitments / due-today work");
        sb.AppendLine("- MyWeek: this week's work");
        sb.AppendLine("- OverdueWork: late items");
        sb.AppendLine("- PersonLens: question about a specific person");
        sb.AppendLine("- InitiativeStatus: question about a specific initiative or project");
        sb.AppendLine("- CaptureSearch: looking for prior captured notes");
        sb.AppendLine("- Generic: anything else");
        sb.AppendLine();
        sb.AppendLine("Respond ONLY with JSON of this shape:");
        sb.AppendLine("{\"intents\":[\"<bucket>\", ...]}");
        sb.AppendLine("Do not include any other text. Use Generic when uncertain.");
        return sb.ToString();
    }

    private static IntentSet? ParseEnvelope(string content)
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
            var envelope = JsonSerializer.Deserialize<IntentEnvelope>(json, JsonOpts);
            if (envelope?.Intents is null || envelope.Intents.Count == 0)
                return IntentSet.Generic;

            var parsed = envelope.Intents
                .Select(i => Enum.TryParse<ChatIntent>(i, ignoreCase: true, out var intent) ? intent : (ChatIntent?)null)
                .Where(i => i is not null)
                .Select(i => i!.Value)
                .Distinct()
                .ToList();

            return parsed.Count == 0
                ? IntentSet.Generic
                : new IntentSet(parsed, EntityHints.Empty);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record IntentEnvelope(List<string>? Intents);
}
