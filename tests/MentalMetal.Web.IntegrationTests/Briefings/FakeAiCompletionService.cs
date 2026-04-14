using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;
using MentalMetal.Domain.Briefings; // type alias to ensure no clash
using MentalMetal.Infrastructure.Ai;

namespace MentalMetal.Web.IntegrationTests.Briefings;

/// <summary>
/// Test double for IAiCompletionService. Each test can override behaviour by
/// reassigning <see cref="ResponderAsync"/>; the default returns a canned briefing.
/// </summary>
public sealed class FakeAiCompletionService : IAiCompletionService
{
    public Func<AiCompletionRequest, CancellationToken, Task<AiCompletionResult>> ResponderAsync { get; set; } =
        (_, _) => Task.FromResult(new AiCompletionResult(
            Content: "# Briefing\n\nFocus today: ship the spec.",
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
