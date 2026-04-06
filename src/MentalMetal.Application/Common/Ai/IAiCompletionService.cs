using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Common.Ai;

public interface IAiCompletionService
{
    Task<AiCompletionResult> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken);
}

public sealed record AiCompletionRequest(
    string SystemPrompt,
    string UserPrompt,
    int? MaxTokens = null,
    float? Temperature = null);

public sealed record AiCompletionResult(
    string Content,
    int InputTokens,
    int OutputTokens,
    string Model,
    AiProvider Provider);
