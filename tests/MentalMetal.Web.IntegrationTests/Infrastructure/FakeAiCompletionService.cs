using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;
using MentalMetal.Infrastructure.Ai;

namespace MentalMetal.Web.IntegrationTests.Infrastructure;

/// <summary>
/// Test double for IAiCompletionService.
/// </summary>
public sealed class FakeAiCompletionService : IAiCompletionService
{
    public Func<AiCompletionRequest, CancellationToken, Task<AiCompletionResult>> ResponderAsync { get; set; } =
        (_, _) => Task.FromResult(new AiCompletionResult(
            Content: "# Test\n\nFake AI response.",
            InputTokens: 100,
            OutputTokens: 50,
            Model: "test-model",
            Provider: AiProvider.Anthropic));

    public int CallCount { get; private set; }

    public Task<AiCompletionResult> CompleteAsync(
        AiCompletionRequest request, CancellationToken cancellationToken)
    {
        CallCount++;
        return ResponderAsync(request, cancellationToken);
    }

    public void ThrowAiNotConfigured() =>
        ResponderAsync = (_, _) => throw new InvalidOperationException(
            "AI provider is not configured. Please set up your AI provider in settings.");
}
