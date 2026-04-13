using MentalMetal.Application.Chat.Global;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Chat.Global;

public class GlobalChatCompletionServiceTests
{
    private readonly IIntentClassifier _classifier = Substitute.For<IIntentClassifier>();
    private readonly IGlobalChatContextBuilder _contextBuilder = Substitute.For<IGlobalChatContextBuilder>();
    private readonly IAiCompletionService _ai = Substitute.For<IAiCompletionService>();
    private readonly GlobalChatCompletionService _svc;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _personId = Guid.NewGuid();
    private readonly Guid _commitmentId = Guid.NewGuid();

    public GlobalChatCompletionServiceTests()
    {
        _svc = new GlobalChatCompletionService(_classifier, _contextBuilder, _ai);
    }

    private ChatThread ThreadWithUserMessage(string content = "What's overdue?")
    {
        var t = ChatThread.Start(_userId, ContextScope.Global());
        t.AppendUserMessage(content);
        return t;
    }

    private GlobalChatContextPayload MakePayload()
    {
        return new GlobalChatContextPayload(
            new IntentSet([ChatIntent.OverdueWork], EntityHints.Empty),
            new GlobalCounters(1, 0, 0),
            Persons: [new PersonContextItem(_personId, "Jane Doe", "Direct", null, null)],
            Initiatives: [],
            Commitments: [new CommitmentContextItem(_commitmentId, "ship deck", "MineToThem", "Jane Doe", "Open", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), true)],
            Delegations: [],
            Captures: [],
            TruncationNotes: [],
            ConversationHistory: []);
    }

    [Fact]
    public async Task HappyPath_PersonCitation_AppendsAssistantWithReference()
    {
        var thread = ThreadWithUserMessage("How is Jane doing?");
        _classifier.ClassifyAsync(_userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IntentSet([ChatIntent.PersonLens], new EntityHints([_personId], [], null)));
        _contextBuilder.BuildAsync(_userId, Arg.Any<IntentSet>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(MakePayload());

        var envelope = $$"""{"assistantText":"Jane is on track.","sourceReferences":[{"entityType":"Person","entityId":"{{_personId}}"}]}""";
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult(envelope, 5, 5, "m", AiProvider.OpenAI));

        await _svc.GenerateReplyAsync(_userId, thread, CancellationToken.None);

        var last = thread.Messages[^1];
        Assert.Equal(ChatRole.Assistant, last.Role);
        Assert.Equal("Jane is on track.", last.Content);
        var reference = Assert.Single(last.SourceReferences);
        Assert.Equal(SourceReferenceEntityType.Person, reference.EntityType);
        Assert.Equal(_personId, reference.EntityId);
    }

    [Fact]
    public async Task HallucinatedSourceReference_IsDropped()
    {
        var thread = ThreadWithUserMessage();
        _classifier.ClassifyAsync(_userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IntentSet([ChatIntent.OverdueWork], EntityHints.Empty));
        _contextBuilder.BuildAsync(_userId, Arg.Any<IntentSet>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(MakePayload());

        var bogus = Guid.NewGuid();
        var envelope = $$"""{"assistantText":"ok","sourceReferences":[{"entityType":"Commitment","entityId":"{{bogus}}"},{"entityType":"Commitment","entityId":"{{_commitmentId}}"}]}""";
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult(envelope, 1, 1, "m", AiProvider.OpenAI));

        await _svc.GenerateReplyAsync(_userId, thread, CancellationToken.None);

        var last = thread.Messages[^1];
        var reference = Assert.Single(last.SourceReferences);
        Assert.Equal(_commitmentId, reference.EntityId);
    }

    [Fact]
    public async Task AiProviderException_AppendsFriendlyAssistantMessage()
    {
        var thread = ThreadWithUserMessage();
        _classifier.ClassifyAsync(_userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IntentSet([ChatIntent.OverdueWork], EntityHints.Empty));
        _contextBuilder.BuildAsync(_userId, Arg.Any<IntentSet>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(MakePayload());
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<AiCompletionResult>>(_ => throw new AiProviderException(AiProvider.OpenAI, 500, "boom"));

        await _svc.GenerateReplyAsync(_userId, thread, CancellationToken.None);

        var last = thread.Messages[^1];
        Assert.Equal(ChatRole.Assistant, last.Role);
        Assert.Contains("AI service", last.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TasteLimitExceeded_AppendsSystemMessage()
    {
        var thread = ThreadWithUserMessage();
        _classifier.ClassifyAsync(_userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IntentSet([ChatIntent.OverdueWork], EntityHints.Empty));
        _contextBuilder.BuildAsync(_userId, Arg.Any<IntentSet>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(MakePayload());
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<AiCompletionResult>>(_ => throw new TasteLimitExceededException());

        await _svc.GenerateReplyAsync(_userId, thread, CancellationToken.None);

        var last = thread.Messages[^1];
        Assert.Equal(ChatRole.System, last.Role);
        Assert.Equal("Daily AI limit reached", last.Content);
    }

    [Fact]
    public async Task RejectsInitiativeScopedThread()
    {
        var thread = ChatThread.Start(_userId, ContextScope.Initiative(Guid.NewGuid()));
        thread.AppendUserMessage("hi");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.GenerateReplyAsync(_userId, thread, CancellationToken.None));
    }
}
