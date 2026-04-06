using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;

namespace MentalMetal.Infrastructure.Ai;

public sealed class GoogleAdapter : IAiProviderAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<AiCompletionResult> CompleteAsync(
        string apiKey,
        string model,
        AiCompletionRequest request,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

        var body = new GeminiRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = [new GeminiPart { Text = request.SystemPrompt }]
            },
            Contents =
            [
                new GeminiContent
                {
                    Role = "user",
                    Parts = [new GeminiPart { Text = request.UserPrompt }]
                }
            ],
            GenerationConfig = new GeminiGenerationConfig
            {
                MaxOutputTokens = request.MaxTokens,
                Temperature = request.Temperature
            }
        };

        var response = await httpClient.PostAsJsonAsync(url, body, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new AiProviderException(
                AiProvider.Google,
                (int)response.StatusCode,
                $"Gemini API error ({response.StatusCode}): {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(JsonOptions, cancellationToken)
            ?? throw new AiProviderException(AiProvider.Google, null, "Empty response from Gemini API.");

        var text = result.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";
        var inputTokens = result.UsageMetadata?.PromptTokenCount ?? 0;
        var outputTokens = result.UsageMetadata?.CandidatesTokenCount ?? 0;

        return new AiCompletionResult(
            Content: text,
            InputTokens: inputTokens,
            OutputTokens: outputTokens,
            Model: model,
            Provider: AiProvider.Google);
    }

    // Gemini API request/response models
    private sealed class GeminiRequest
    {
        public GeminiContent? SystemInstruction { get; set; }
        public List<GeminiContent> Contents { get; set; } = [];
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    private sealed class GeminiContent
    {
        public string? Role { get; set; }
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private sealed class GeminiPart
    {
        public string? Text { get; set; }
    }

    private sealed class GeminiGenerationConfig
    {
        public int? MaxOutputTokens { get; set; }
        public float? Temperature { get; set; }
    }

    private sealed class GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
        public GeminiUsageMetadata? UsageMetadata { get; set; }
    }

    private sealed class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    private sealed class GeminiUsageMetadata
    {
        public int PromptTokenCount { get; set; }
        public int CandidatesTokenCount { get; set; }
    }
}
