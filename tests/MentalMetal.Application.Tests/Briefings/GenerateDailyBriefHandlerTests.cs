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
    private readonly IBriefCacheService _cacheService = Substitute.For<IBriefCacheService>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly GenerateDailyBriefHandler _sut;

    public GenerateDailyBriefHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _sut = new GenerateDailyBriefHandler(
            _captureRepo, _commitmentRepo, _personRepo, _aiService, _currentUser, _cacheService);
    }

    private void SetupEmptyDataSources()
    {
        _captureRepo.GetByDateRangeAsync(
            _userId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture>());
        _commitmentRepo.GetAllAsync(_userId, null, CommitmentStatus.Open, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());
        _personRepo.GetByIdsAsync(_userId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Person>());
        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("No captures from yesterday.", 20, 50, "test-model", AiProvider.Anthropic));
    }

    [Fact]
    public async Task HandleAsync_NoCapturesYesterday_ReturnsEmptyBrief()
    {
        SetupEmptyDataSources();

        var result = await _sut.HandleAsync(forceRefresh: true, CancellationToken.None);

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
        SetupEmptyDataSources();

        await _sut.HandleAsync(forceRefresh: true, CancellationToken.None);

        await _aiService.Received(1).CompleteAsync(
            Arg.Is<AiCompletionRequest>(r => r.SystemPrompt.Contains("daily briefing")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsGeneratedAtTimestamp()
    {
        SetupEmptyDataSources();

        var before = DateTimeOffset.UtcNow;
        var result = await _sut.HandleAsync(forceRefresh: true, CancellationToken.None);

        Assert.True(result.GeneratedAt >= before);
    }

    [Fact]
    public async Task HandleAsync_CaptureWithNullExtractionCollections_DoesNotThrow()
    {
        // Regression: EF Core JSON deserialization can produce null for Decisions/Risks
        // even when C# defaults are set. The handler must tolerate this.
        var capture = Capture.Create(_userId, "Meeting notes.", CaptureType.MeetingNotes);
        capture.BeginProcessing();
        capture.CompleteProcessing(new AiExtraction
        {
            Summary = "A meeting.",
            Decisions = null!,
            Risks = null!,
            ExtractedAt = DateTimeOffset.UtcNow
        });

        _captureRepo.GetByDateRangeAsync(
            _userId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture> { capture });
        _commitmentRepo.GetAllAsync(_userId, null, CommitmentStatus.Open, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());
        _personRepo.GetByIdsAsync(_userId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Person>());

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("Brief.", 20, 50, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(forceRefresh: true, CancellationToken.None);

        Assert.NotNull(result.Narrative);
        await _aiService.Received(1).CompleteAsync(
            Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsCachedBrief_WhenNotForceRefresh()
    {
        var cached = new DailyBriefResponse(
            "Cached narrative", [], [], [], [], 5, DateTimeOffset.UtcNow.AddMinutes(-30));
        _cacheService.GetDailyBrief(_userId).Returns(cached);

        var result = await _sut.HandleAsync(forceRefresh: false, CancellationToken.None);

        Assert.Same(cached, result);
        await _aiService.DidNotReceive().CompleteAsync(
            Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_BypassesCache_WhenForceRefresh()
    {
        var cached = new DailyBriefResponse(
            "Cached narrative", [], [], [], [], 5, DateTimeOffset.UtcNow.AddMinutes(-30));
        _cacheService.GetDailyBrief(_userId).Returns(cached);
        SetupEmptyDataSources();

        var result = await _sut.HandleAsync(forceRefresh: true, CancellationToken.None);

        Assert.NotSame(cached, result);
        await _aiService.Received(1).CompleteAsync(
            Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CachesGeneratedBrief()
    {
        SetupEmptyDataSources();

        var result = await _sut.HandleAsync(forceRefresh: true, CancellationToken.None);

        _cacheService.Received(1).SetDailyBrief(_userId, result);
    }

    [Fact]
    public async Task HandleAsync_UsesDateRangeQuery_NotGetAllAsync()
    {
        SetupEmptyDataSources();

        await _sut.HandleAsync(forceRefresh: true, CancellationToken.None);

        await _captureRepo.Received(1).GetByDateRangeAsync(
            _userId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            ProcessingStatus.Processed, Arg.Any<CancellationToken>());
        await _captureRepo.DidNotReceive().GetAllAsync(
            Arg.Any<Guid>(), Arg.Any<CaptureType?>(), Arg.Any<ProcessingStatus?>(),
            Arg.Any<CancellationToken>());
    }
}
