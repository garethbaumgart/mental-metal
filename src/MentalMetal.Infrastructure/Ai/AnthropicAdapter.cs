using Anthropic;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;

namespace MentalMetal.Infrastructure.Ai;

public sealed class AnthropicAdapter : IAiProviderAdapter
{
    public async Task<AiCompletionResult> CompleteAsync(
        string apiKey,
        string model,
        AiCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var client = new AnthropicClient(apiKey: apiKey);

        var messages = new List<InputMessage>
        {
            new()
            {
                Role = InputMessageRole.User,
                Content = request.UserPrompt
            }
        };

        try
        {
            var response = await client.Messages.MessagesPostAsync(
                model: model,
                messages: messages,
                maxTokens: request.MaxTokens ?? 1024,
                system: request.SystemPrompt,
                temperature: request.Temperature.HasValue ? (double)request.Temperature.Value : null,
                cancellationToken: cancellationToken);

            var firstBlock = response.Content.FirstOrDefault();
            var text = firstBlock.IsText ? firstBlock.Text.Text : "";

            return new AiCompletionResult(
                Content: text,
                InputTokens: response.Usage.InputTokens,
                OutputTokens: response.Usage.OutputTokens,
                Model: response.Model.ToString(),
                Provider: AiProvider.Anthropic);
        }
        catch (ApiException ex)
        {
            throw new AiProviderException(
                AiProvider.Anthropic,
                (int)ex.StatusCode,
                $"Anthropic API error: {ex.Message}");
        }
    }
}
