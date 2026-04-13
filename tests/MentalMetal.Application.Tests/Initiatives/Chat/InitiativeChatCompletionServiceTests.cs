using MentalMetal.Application.Common.Ai;
using MentalMetal.Application.Initiatives.Chat;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Initiatives.Chat;

public class InitiativeChatCompletionServiceTests
{
    private readonly IInitiativeChatContextBuilder _contextBuilder = Substitute.For<IInitiativeChatContextBuilder>();
    private readonly IAiCompletionService _ai = Substitute.For<IAiCompletionService>();
    private readonly InitiativeChatCompletionService _svc;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _initiativeId = Guid.NewGuid();
    private readonly Guid _decisionId = Guid.NewGuid();

    public InitiativeChatCompletionServiceTests()
    {
        _svc = new InitiativeChatCompletionService(_contextBuilder, _ai);
    }

    private ChatThread CreateThreadWithUserMessage(string content = "What did we decide?")
    {
        var thread = ChatThread.Start(_userId, ContextScope.Initiative(_initiativeId));
        thread.AppendUserMessage(content);
        return thread;
    }

    private InitiativeChatContextPayload ContextWith(Guid decisionId)
    {
        return new InitiativeChatContextPayload(
            new InitiativeMetadataContext(_initiativeId, "Init", "Active", []),
            new LivingBriefContext(
                "summary",
                1,
                DateTimeOffset.UtcNow,
                RecentDecisionIds: [decisionId],
                RecentDecisions: [new LivingBriefDecisionContext(decisionId, "Adopt Postgres", null, DateTimeOffset.UtcNow)],
                OpenRiskIds: [],
                OpenRisks: [],
                LatestRequirementsId: null,
                LatestRequirementsContent: null,
                LatestDesignDirectionId: null,
                LatestDesignDirectionContent: null),
            Commitments: [],
            Delegations: [],
            LinkedCaptures: [],
            ConversationHistory: []);
    }

    [Fact]
    public async Task HappyPath_ParsesEnvelope_AndAppendsAssistantWithCitations()
    {
        var thread = CreateThreadWithUserMessage();
        var payload = ContextWith(_decisionId);
        _contextBuilder.BuildAsync(_userId, _initiativeId, Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(payload);

        var envelope = $$"""{"assistantText":"We chose Postgres.","sourceReferences":[{"entityType":"LivingBriefDecision","entityId":"{{_decisionId}}"}]}""";
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult(envelope, 10, 20, "gpt-test", AiProvider.OpenAI));

        await _svc.GenerateReplyAsync(_userId, thread, CancellationToken.None);

        var last = thread.Messages[^1];
        Assert.Equal(ChatRole.Assistant, last.Role);
        Assert.Equal("We chose Postgres.", last.Content);
        var refSingle = Assert.Single(last.SourceReferences);
        Assert.Equal(SourceReferenceEntityType.LivingBriefDecision, refSingle.EntityType);
        Assert.Equal(_decisionId, refSingle.EntityId);
        Assert.NotNull(last.TokenUsage);
    }

    [Fact]
    public async Task MalformedEnvelope_FallsBackToRawText_WithNoCitations()
    {
        var thread = CreateThreadWithUserMessage();
        _contextBuilder.BuildAsync(_userId, _initiativeId, Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(ContextWith(_decisionId));

        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("this is not JSON at all", 5, 5, "m", AiProvider.OpenAI));

        await _svc.GenerateReplyAsync(_userId, thread, CancellationToken.None);

        var last = thread.Messages[^1];
        Assert.Equal(ChatRole.Assistant, last.Role);
        Assert.Equal("this is not JSON at all", last.Content);
        Assert.Empty(last.SourceReferences);
    }

    [Fact]
    public async Task InvalidCitations_AreDroppedBeforePersisting()
    {
        var thread = CreateThreadWithUserMessage();
        _contextBuilder.BuildAsync(_userId, _initiativeId, Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(ContextWith(_decisionId));

        var bogusId = Guid.NewGuid();
        var envelope = $$"""{"assistantText":"ok","sourceReferences":[{"entityType":"LivingBriefDecision","entityId":"{{bogusId}}"},{"entityType":"LivingBriefDecision","entityId":"{{_decisionId}}"}]}""";
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult(envelope, 1, 1, "m", AiProvider.OpenAI));

        await _svc.GenerateReplyAsync(_userId, thread, CancellationToken.None);

        var last = thread.Messages[^1];
        var reference = Assert.Single(last.SourceReferences);
        Assert.Equal(_decisionId, reference.EntityId);
    }

    [Fact]
    public async Task AiProviderException_AppendsFriendlyAssistantMessage()
    {
        var thread = CreateThreadWithUserMessage();
        _contextBuilder.BuildAsync(_userId, _initiativeId, Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(ContextWith(_decisionId));

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
        var thread = CreateThreadWithUserMessage();
        _contextBuilder.BuildAsync(_userId, _initiativeId, Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(ContextWith(_decisionId));

        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<AiCompletionResult>>(_ => throw new TasteLimitExceededException());

        await _svc.GenerateReplyAsync(_userId, thread, CancellationToken.None);

        var last = thread.Messages[^1];
        Assert.Equal(ChatRole.System, last.Role);
        Assert.Equal("Daily AI limit reached", last.Content);
    }
}
