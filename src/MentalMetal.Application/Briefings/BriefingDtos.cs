using System.Text.Json;
using System.Text.Json.Serialization;
using MentalMetal.Domain.Briefings;

namespace MentalMetal.Application.Briefings;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BriefingTypeDto
{
    Morning,
    Weekly,
    OneOnOnePrep,
}

public sealed record BriefingResponse(
    Guid Id,
    BriefingTypeDto Type,
    string ScopeKey,
    DateTimeOffset GeneratedAtUtc,
    string MarkdownBody,
    string Model,
    int InputTokens,
    int OutputTokens,
    JsonElement FactsSummary);

public sealed record BriefingSummary(
    Guid Id,
    BriefingTypeDto Type,
    string ScopeKey,
    DateTimeOffset GeneratedAtUtc,
    string Model,
    int InputTokens,
    int OutputTokens);

public static class BriefingMappings
{
    public static BriefingResponse ToResponse(this Briefing b) => new(
        b.Id,
        (BriefingTypeDto)b.Type,
        b.ScopeKey,
        b.GeneratedAtUtc,
        b.MarkdownBody,
        b.Model,
        b.InputTokens,
        b.OutputTokens,
        ParseFacts(b.PromptFactsJson));

    public static BriefingSummary ToSummary(this Briefing b) => new(
        b.Id,
        (BriefingTypeDto)b.Type,
        b.ScopeKey,
        b.GeneratedAtUtc,
        b.Model,
        b.InputTokens,
        b.OutputTokens);

    private static JsonElement ParseFacts(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
