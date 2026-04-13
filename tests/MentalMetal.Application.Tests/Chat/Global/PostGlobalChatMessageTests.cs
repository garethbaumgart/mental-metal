using MentalMetal.Application.Chat.Global;
using MentalMetal.Application.Common;
using MentalMetal.Application.Initiatives.Chat;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Chat.Global;

public class PostGlobalChatMessageTests
{
    private readonly IChatThreadRepository _threads = Substitute.For<IChatThreadRepository>();
    private readonly IGlobalChatCompletionService _completion = Substitute.For<IGlobalChatCompletionService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly PostGlobalChatMessageHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public PostGlobalChatMessageTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new PostGlobalChatMessageHandler(_threads, _completion, _currentUser, _uow);
    }

    [Fact]
    public async Task Handle_WhenThreadMissing_ReturnsNull()
    {
        _threads.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ChatThread?)null);
        var resp = await _handler.HandleAsync(Guid.NewGuid(), new PostChatMessageRequest("hi"), CancellationToken.None);
        Assert.Null(resp);
    }

    [Fact]
    public async Task Handle_WhenThreadBelongsToOtherUser_ReturnsNull()
    {
        // Cross-user access: thread owned by another user → 404 (return null).
        var other = ChatThread.Start(Guid.NewGuid(), ContextScope.Global());
        _threads.GetByIdAsync(other.Id, Arg.Any<CancellationToken>()).Returns(other);

        var resp = await _handler.HandleAsync(other.Id, new PostChatMessageRequest("hi"), CancellationToken.None);

        Assert.Null(resp);
        await _completion.DidNotReceiveWithAnyArgs().GenerateReplyAsync(default, default!, default);
    }

    [Fact]
    public async Task Handle_WhenThreadIsInitiativeScoped_ReturnsNull()
    {
        // 8.4: a thread fetched via /api/chat must reject initiative-scoped threads.
        var thread = ChatThread.Start(_userId, ContextScope.Initiative(Guid.NewGuid()));
        _threads.GetByIdAsync(thread.Id, Arg.Any<CancellationToken>()).Returns(thread);

        var resp = await _handler.HandleAsync(thread.Id, new PostChatMessageRequest("hi"), CancellationToken.None);

        Assert.Null(resp);
    }

    [Fact]
    public async Task Handle_WhenThreadArchived_ThrowsArchivedThreadException()
    {
        var thread = ChatThread.Start(_userId, ContextScope.Global());
        thread.Archive();
        _threads.GetByIdAsync(thread.Id, Arg.Any<CancellationToken>()).Returns(thread);

        await Assert.ThrowsAsync<PostGlobalChatMessageHandler.ArchivedThreadException>(() =>
            _handler.HandleAsync(thread.Id, new PostChatMessageRequest("hi"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_HappyPath_AppendsUserAndAssistantMessages_AndPersists()
    {
        var thread = ChatThread.Start(_userId, ContextScope.Global());
        _threads.GetByIdAsync(thread.Id, Arg.Any<CancellationToken>()).Returns(thread);

        _completion.GenerateReplyAsync(_userId, thread, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                thread.AppendAssistantMessage("Here is the answer.", [], new TokenUsage(1, 1));
                return Task.CompletedTask;
            });

        var resp = await _handler.HandleAsync(thread.Id, new PostChatMessageRequest("What is overdue?"), CancellationToken.None);

        Assert.NotNull(resp);
        Assert.Equal("What is overdue?", resp!.UserMessage.Content);
        Assert.Equal("Assistant", resp.AssistantMessage.Role);
        Assert.Equal(2, thread.Messages.Count);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyContent_ThrowsArgumentException()
    {
        var thread = ChatThread.Start(_userId, ContextScope.Global());
        _threads.GetByIdAsync(thread.Id, Arg.Any<CancellationToken>()).Returns(thread);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.HandleAsync(thread.Id, new PostChatMessageRequest("   "), CancellationToken.None));
    }
}
