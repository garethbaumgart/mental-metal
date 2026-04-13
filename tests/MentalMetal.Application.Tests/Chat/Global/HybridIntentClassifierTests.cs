using MentalMetal.Application.Chat.Global;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Chat.Global;

public class HybridIntentClassifierTests
{
    private readonly IPersonRepository _people = Substitute.For<IPersonRepository>();
    private readonly IInitiativeRepository _initiatives = Substitute.For<IInitiativeRepository>();
    private readonly IAiCompletionService _ai = Substitute.For<IAiCompletionService>();
    private readonly HybridIntentClassifier _classifier;
    private readonly Guid _userId = Guid.NewGuid();

    public HybridIntentClassifierTests()
    {
        var rule = new RuleIntentClassifier(_people, _initiatives);
        var aiClassifier = new AiIntentClassifier(_ai);
        _classifier = new HybridIntentClassifier(rule, aiClassifier);
        _people.GetAllAsync(_userId, Arg.Any<PersonType?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Person>());
        _initiatives.GetAllAsync(_userId, Arg.Any<InitiativeStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Initiative>());
    }

    [Fact]
    public async Task RuleHits_AiNotCalled()
    {
        var result = await _classifier.ClassifyAsync(_userId, "What's overdue?", CancellationToken.None);
        Assert.Contains(ChatIntent.OverdueWork, result.Intents);
        await _ai.DidNotReceiveWithAnyArgs().CompleteAsync(default!, default);
    }

    [Fact]
    public async Task RuleMisses_FallsThroughToAi()
    {
        // The rule layer returns Generic for this; AI layer is called. Stubbing AI to return
        // CaptureSearch confirms the orchestration uses the AI result.
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("{\"intents\":[\"CaptureSearch\"]}", 1, 1, "m", AiProvider.OpenAI));

        var result = await _classifier.ClassifyAsync(_userId, "Anything I should be worried about?", CancellationToken.None);

        Assert.Contains(ChatIntent.CaptureSearch, result.Intents);
        await _ai.Received(1).CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AiFails_FallsBackToGeneric()
    {
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<AiCompletionResult>>(_ => throw new AiProviderException(AiProvider.OpenAI, 500, "boom"));

        var result = await _classifier.ClassifyAsync(_userId, "Étrange question en français", CancellationToken.None);

        Assert.True(result.IsGenericOnly);
    }

    [Fact]
    public async Task AiReturnsMalformed_FallsBackToGeneric()
    {
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("not JSON at all", 1, 1, "m", AiProvider.OpenAI));

        var result = await _classifier.ClassifyAsync(_userId, "obscure language fallback", CancellationToken.None);
        Assert.True(result.IsGenericOnly);
    }
}
