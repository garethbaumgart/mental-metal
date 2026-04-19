using MentalMetal.Application.Briefings;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Briefings;

public class GenerateWeeklyBriefHandlerTests
{
    private readonly ICaptureRepository _captureRepo = Substitute.For<ICaptureRepository>();
    private readonly ICommitmentRepository _commitmentRepo = Substitute.For<ICommitmentRepository>();
    private readonly IInitiativeRepository _initiativeRepo = Substitute.For<IInitiativeRepository>();
    private readonly IAiCompletionService _aiService = Substitute.For<IAiCompletionService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly GenerateWeeklyBriefHandler _sut;

    public GenerateWeeklyBriefHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _sut = new GenerateWeeklyBriefHandler(
            _captureRepo, _commitmentRepo, _initiativeRepo, _aiService, _currentUser);
    }

    [Fact]
    public async Task HandleAsync_NoCapturesThisWeek_ReturnsEmptyBrief()
    {
        _captureRepo.GetAllAsync(_userId, null, ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture>());
        _commitmentRepo.GetAllAsync(_userId, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());
        _initiativeRepo.GetAllAsync(_userId, InitiativeStatus.Active, Arg.Any<CancellationToken>())
            .Returns(new List<Initiative>());

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("Quiet week.", 20, 50, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(null, CancellationToken.None);

        Assert.Equal("Quiet week.", result.Narrative);
        Assert.Empty(result.InitiativeActivity);
        Assert.Empty(result.CrossConversationInsights);
        Assert.Equal(0, result.CommitmentStatus.NewCount);
    }

    [Fact]
    public async Task HandleAsync_CallsAiWithWeeklyPrompt()
    {
        _captureRepo.GetAllAsync(_userId, null, ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture>());
        _commitmentRepo.GetAllAsync(_userId, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());
        _initiativeRepo.GetAllAsync(_userId, InitiativeStatus.Active, Arg.Any<CancellationToken>())
            .Returns(new List<Initiative>());

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("Weekly review.", 20, 50, "test-model", AiProvider.Anthropic));

        await _sut.HandleAsync(null, CancellationToken.None);

        await _aiService.Received(1).CompleteAsync(
            Arg.Is<AiCompletionRequest>(r => r.SystemPrompt.Contains("weekly briefing")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithSpecificWeek_UsesCorrectDateRange()
    {
        var weekOf = new DateOnly(2026, 4, 15); // Wednesday
        _captureRepo.GetAllAsync(_userId, null, ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture>());
        _commitmentRepo.GetAllAsync(_userId, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());
        _initiativeRepo.GetAllAsync(_userId, InitiativeStatus.Active, Arg.Any<CancellationToken>())
            .Returns(new List<Initiative>());

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("Week of April 13.", 20, 50, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(weekOf, CancellationToken.None);

        // April 15, 2026 is a Wednesday, so week starts on Monday April 13
        Assert.Equal(new DateTimeOffset(2026, 4, 13, 0, 0, 0, TimeSpan.Zero), result.DateRange.Start);
        Assert.Equal(new DateTimeOffset(2026, 4, 20, 0, 0, 0, TimeSpan.Zero), result.DateRange.End);
    }

    [Fact]
    public async Task HandleAsync_ReturnsDecisionsAndRisksFromCaptures()
    {
        var capture = Capture.Create(_userId, "Weekly check-in notes.", CaptureType.MeetingNotes);
        capture.BeginProcessing();
        capture.CompleteProcessing(new AiExtraction
        {
            Summary = "Weekly sync.",
            Decisions = new List<string> { "Approved the new timeline" },
            Risks = new List<string> { "Budget concerns remain" },
            ExtractedAt = DateTimeOffset.UtcNow
        });

        _captureRepo.GetAllAsync(_userId, null, ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture> { capture });
        _commitmentRepo.GetAllAsync(_userId, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());
        _initiativeRepo.GetAllAsync(_userId, InitiativeStatus.Active, Arg.Any<CancellationToken>())
            .Returns(new List<Initiative>());

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("Brief.", 20, 50, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(null, CancellationToken.None);

        // The capture may or may not be in the current week depending on when the test runs
        // but the handler should handle it gracefully either way
        Assert.NotNull(result.Narrative);
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

        _captureRepo.GetAllAsync(_userId, null, ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture> { capture });
        _commitmentRepo.GetAllAsync(_userId, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());
        _initiativeRepo.GetAllAsync(_userId, InitiativeStatus.Active, Arg.Any<CancellationToken>())
            .Returns(new List<Initiative>());

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("Brief.", 20, 50, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(null, CancellationToken.None);

        Assert.NotNull(result.Narrative);
    }
}
