using System.Text.Json.Serialization;

namespace MentalMetal.Application.Captures.AutoExtract;

/// <summary>
/// DTO matching the JSON schema requested from the AI provider.
/// </summary>
internal sealed record ExtractionResponseDto
{
    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;

    [JsonPropertyName("people_mentioned")]
    public List<PersonMentionDto> PeopleMentioned { get; init; } = [];

    [JsonPropertyName("commitments")]
    public List<CommitmentDto> Commitments { get; init; } = [];

    [JsonPropertyName("decisions")]
    public List<string> Decisions { get; init; } = [];

    [JsonPropertyName("risks")]
    public List<string> Risks { get; init; } = [];

    [JsonPropertyName("initiative_tags")]
    public List<InitiativeTagDto> InitiativeTags { get; init; } = [];
}

internal sealed record PersonMentionDto
{
    [JsonPropertyName("raw_name")]
    public string RawName { get; init; } = string.Empty;

    [JsonPropertyName("context")]
    public string? Context { get; init; }
}

internal sealed record CommitmentDto
{
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("direction")]
    public string? Direction { get; init; }

    [JsonPropertyName("person_raw_name")]
    public string? PersonRawName { get; init; }

    [JsonPropertyName("due_date")]
    public string? DueDate { get; init; }

    [JsonPropertyName("confidence")]
    public string? Confidence { get; init; }
}

internal sealed record InitiativeTagDto
{
    [JsonPropertyName("raw_name")]
    public string RawName { get; init; } = string.Empty;

    [JsonPropertyName("context")]
    public string? Context { get; init; }
}
