using MentalMetal.Application.Common;
using MentalMetal.Application.Initiatives.Chat;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Initiatives.Chat;

public class PostInitiativeChatMessageTests
{
    private readonly IChatThreadRepository _threads = Substitute.For<IChatThreadRepository>();
    private readonly IInitiativeChatCompletionService _completion = Substitute.For<IInitiativeChatCompletionService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly PostInitiativeChatMessageHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _initiativeId = Guid.NewGuid();

    public PostInitiativeChatMessageTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new PostInitiativeChatMessageHandler(_threads, _completion, _currentUser, _uow);
    }

    [Fact]
    public async Task Handle_WhenThreadMissing_ReturnsNull()
    {
        _threads.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ChatThread?)null);
        var resp = await _handler.HandleAsync(_initiativeId, Guid.NewGuid(), new PostChatMessageRequest("hi"), CancellationToken.None);
        Assert.Null(resp);
    }

    [Fact]
    public async Task Handle_WhenThreadBelongsToOtherUser_ReturnsNull()
    {
        var other = ChatThread.Start(Guid.NewGuid(), ContextScope.Initiative(_initiativeId));
        _threads.GetByIdAsync(other.Id, Arg.Any<CancellationToken>()).Returns(other);

        var resp = await _handler.HandleAsync(_initiativeId, other.Id, new PostChatMessageRequest("hi"), CancellationToken.None);

        Assert.Null(resp);
        await _completion.DidNotReceiveWithAnyArgs().GenerateReplyAsync(default, default!, default);
    }

    [Fact]
    public async Task Handle_WhenThreadScopedToAnotherInitiative_ReturnsNull()
    {
        var thread = ChatThread.Start(_userId, ContextScope.Initiative(Guid.NewGuid()));
        _threads.GetByIdAsync(thread.Id, Arg.Any<CancellationToken>()).Returns(thread);

        var resp = await _handler.HandleAsync(_initiativeId, thread.Id, new PostChatMessageRequest("hi"), CancellationToken.None);

        Assert.Null(resp);
    }

    [Fact]
    public async Task Handle_WhenThreadArchived_ThrowsArchivedThreadException()
    {
        var thread = ChatThread.Start(_userId, ContextScope.Initiative(_initiativeId));
        thread.Archive();
        _threads.GetByIdAsync(thread.Id, Arg.Any<CancellationToken>()).Returns(thread);

        await Assert.ThrowsAsync<PostInitiativeChatMessageHandler.ArchivedThreadException>(() =>
            _handler.HandleAsync(_initiativeId, thread.Id, new PostChatMessageRequest("hi"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_HappyPath_AppendsUserAndAssistantMessages_AndReturnsBoth()
    {
        var thread = ChatThread.Start(_userId, ContextScope.Initiative(_initiativeId));
        _threads.GetByIdAsync(thread.Id, Arg.Any<CancellationToken>()).Returns(thread);

        _completion.GenerateReplyAsync(_userId, thread, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                thread.AppendAssistantMessage("Here is the answer.", [], new TokenUsage(1, 1));
                return Task.CompletedTask;
            });

        var resp = await _handler.HandleAsync(_initiativeId, thread.Id, new PostChatMessageRequest("What do we know?"), CancellationToken.None);

        Assert.NotNull(resp);
        Assert.Equal("What do we know?", resp!.UserMessage.Content);
        Assert.Equal("Assistant", resp.AssistantMessage.Role);
        Assert.Equal(2, thread.Messages.Count);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyContent_ThrowsArgumentException()
    {
        var thread = ChatThread.Start(_userId, ContextScope.Initiative(_initiativeId));
        _threads.GetByIdAsync(thread.Id, Arg.Any<CancellationToken>()).Returns(thread);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.HandleAsync(_initiativeId, thread.Id, new PostChatMessageRequest("   "), CancellationToken.None));
    }
}
