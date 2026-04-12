using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;

namespace MentalMetal.Infrastructure.Ai;

public sealed class AiProviderValidator(
    AnthropicAdapter anthropicAdapter,
    OpenAiAdapter openAiAdapter,
    GoogleAdapter googleAdapter) : IAiProviderValidator
{
    public async Task<string> ValidateAsync(
        AiProvider provider,
        string apiKey,
        string model,
        CancellationToken cancellationToken)
    {
        var adapter = GetAdapter(provider);

        var testRequest = new AiCompletionRequest(
            SystemPrompt: "You are a test. Respond with exactly: OK",
            UserPrompt: "Test connection.",
            MaxTokens: 10);

        var result = await adapter.CompleteAsync(apiKey, model, testRequest, cancellationToken);
        return result.Model;
    }

    private IAiProviderAdapter GetAdapter(AiProvider provider) => provider switch
    {
        AiProvider.Anthropic => anthropicAdapter,
        AiProvider.OpenAI => openAiAdapter,
        AiProvider.Google => googleAdapter,
        _ => throw new ArgumentOutOfRangeException(nameof(provider))
    };
}
