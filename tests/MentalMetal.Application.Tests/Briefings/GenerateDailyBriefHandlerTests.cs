using MentalMetal.Application.Briefings;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Briefings;

public class GenerateDailyBriefHandlerTests
{
    private readonly ICaptureRepository _captureRepo = Substitute.For<ICaptureRepository>();
    private readonly ICommitmentRepository _commitmentRepo = Substitute.For<ICommitmentRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly IAiCompletionService _aiService = Substitute.For<IAiCompletionService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly GenerateDailyBriefHandler _sut;

    public GenerateDailyBriefHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _sut = new GenerateDailyBriefHandler(
            _captureRepo, _commitmentRepo, _personRepo, _aiService, _currentUser);
    }

    [Fact]
    public async Task HandleAsync_NoCapturesYesterday_ReturnsEmptyBrief()
    {
        _captureRepo.GetAllAsync(_userId, null, ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture>());
        _commitmentRepo.GetAllAsync(_userId, null, CommitmentStatus.Open, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());
        _personRepo.GetByIdsAsync(_userId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Person>());

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("No captures from yesterday.", 20, 50, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(CancellationToken.None);

        Assert.Equal("No captures from yesterday.", result.Narrative);
        Assert.Equal(0, result.CaptureCount);
        Assert.Empty(result.FreshCommitments);
        Assert.Empty(result.DueToday);
        Assert.Empty(result.Overdue);
        Assert.Empty(result.PeopleActivity);
    }

    [Fact]
    public async Task HandleAsync_CallsAiWithCorrectSystemPrompt()
    {
        _captureRepo.GetAllAsync(_userId, null, ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture>());
        _commitmentRepo.GetAllAsync(_userId, null, CommitmentStatus.Open, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());
        _personRepo.GetByIdsAsync(_userId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Person>());

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("Brief content.", 20, 50, "test-model", AiProvider.Anthropic));

        await _sut.HandleAsync(CancellationToken.None);

        await _aiService.Received(1).CompleteAsync(
            Arg.Is<AiCompletionRequest>(r => r.SystemPrompt.Contains("daily briefing")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsGeneratedAtTimestamp()
    {
        _captureRepo.GetAllAsync(_userId, null, ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture>());
        _commitmentRepo.GetAllAsync(_userId, null, CommitmentStatus.Open, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());
        _personRepo.GetByIdsAsync(_userId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Person>());

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("Brief.", 20, 50, "test-model", AiProvider.Anthropic));

        var before = DateTimeOffset.UtcNow;
        var result = await _sut.HandleAsync(CancellationToken.None);

        Assert.True(result.GeneratedAt >= before);
    }
}
