using System.ClientModel;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;
using OpenAI.Chat;

namespace MentalMetal.Infrastructure.Ai;

public sealed class OpenAiAdapter : IAiProviderAdapter
{
    public async Task<AiCompletionResult> CompleteAsync(
        string apiKey,
        string model,
        AiCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var client = new ChatClient(model, apiKey);

        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateSystemMessage(request.SystemPrompt),
            ChatMessage.CreateUserMessage(request.UserPrompt)
        };

        var options = new ChatCompletionOptions();
        if (request.MaxTokens.HasValue)
            options.MaxOutputTokenCount = request.MaxTokens.Value;
        if (request.Temperature.HasValue)
            options.Temperature = request.Temperature.Value;

        try
        {
            var response = await client.CompleteChatAsync(messages, options, cancellationToken);

            return new AiCompletionResult(
                Content: response.Value.Content[0].Text,
                InputTokens: response.Value.Usage.InputTokenCount,
                OutputTokens: response.Value.Usage.OutputTokenCount,
                Model: response.Value.Model ?? model,
                Provider: AiProvider.OpenAI);
        }
        catch (ClientResultException ex)
        {
            throw new AiProviderException(
                AiProvider.OpenAI,
                ex.Status,
                $"OpenAI API error: {ex.Message}");
        }
    }
}
