using MentalMetal.Application.Common.Ai;

namespace MentalMetal.Infrastructure.Ai;

public interface IAiProviderAdapter
{
    Task<AiCompletionResult> CompleteAsync(
        string apiKey,
        string model,
        AiCompletionRequest request,
        CancellationToken cancellationToken);
}
