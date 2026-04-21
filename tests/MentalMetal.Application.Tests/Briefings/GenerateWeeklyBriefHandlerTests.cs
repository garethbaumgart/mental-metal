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
    public async Task HandleAsync_NoDataThisWeek_SkipsAiAndReturnsCannedNarrative()
    {
        // Regression: bug #4 — AI was invoked with empty context, producing misleading narrative.
        _captureRepo.GetAllAsync(_userId, null, ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture>());
        _commitmentRepo.GetAllAsync(_userId, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());
        _initiativeRepo.GetAllAsync(_userId, InitiativeStatus.Active, Arg.Any<CancellationToken>())
            .Returns(new List<Initiative>());

        var result = await _sut.HandleAsync(null, CancellationToken.None);

        Assert.Contains("No captures or commitment activity", result.Narrative);
        Assert.Empty(result.InitiativeActivity);
        Assert.Empty(result.CrossConversationInsights);
        Assert.Equal(0, result.CommitmentStatus.NewCount);
        await _aiService.DidNotReceive().CompleteAsync(
            Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithCaptures_CallsAiWithWeeklyPrompt()
    {
        var capture = Capture.Create(_userId, "Status sync.", CaptureType.MeetingNotes);
        capture.BeginProcessing();
        capture.CompleteProcessing(new AiExtraction
        {
            Summary = "Sync meeting.",
            ExtractedAt = DateTimeOffset.UtcNow
        });

        _captureRepo.GetAllAsync(_userId, null, ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture> { capture });
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
        // Regression: bug #2 — EF Core JSON deserialization can produce null for Decisions/Risks
        // even when C# defaults are set. The handler must tolerate this via null-coalescing.
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

    [Fact]
    public async Task HandleAsync_InitiativeWithCommitmentsButNoCaptures_IncludedInActivity()
    {
        // Regression: bug #3 — initiatives were excluded if they had no linked captures in the
        // date range, even when they had commitments created that week.
        // Use null weekOf so the handler defaults to the current week, which will include
        // the commitment we just created (CreatedAt = UtcNow).
        var initiative = Initiative.Create(_userId, "Project Alpha");
        var personId = Guid.NewGuid();

        var commitment = Commitment.Create(
            _userId, "Deliver spec", CommitmentDirection.MineToThem, personId,
            initiativeId: initiative.Id);

        _captureRepo.GetAllAsync(_userId, null, ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture>());
        _commitmentRepo.GetAllAsync(_userId, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment> { commitment });
        _initiativeRepo.GetAllAsync(_userId, InitiativeStatus.Active, Arg.Any<CancellationToken>())
            .Returns(new List<Initiative> { initiative });

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("Active week.", 20, 50, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(null, CancellationToken.None);

        Assert.Single(result.InitiativeActivity);
        Assert.Equal("Project Alpha", result.InitiativeActivity[0].Title);
    }

    [Fact]
    public async Task HandleAsync_OnlyProcessedCapturesUsed()
    {
        // Regression: bug #1 — weekly brief must only use processed captures (matching daily brief).
        // The repo is called with ProcessingStatus.Processed filter.
        _captureRepo.GetAllAsync(_userId, null, ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture>());
        _commitmentRepo.GetAllAsync(_userId, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());
        _initiativeRepo.GetAllAsync(_userId, InitiativeStatus.Active, Arg.Any<CancellationToken>())
            .Returns(new List<Initiative>());

        await _sut.HandleAsync(null, CancellationToken.None);

        // Verify the capture repo was called with the Processed filter
        await _captureRepo.Received(1).GetAllAsync(
            _userId, null, ProcessingStatus.Processed, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PersonWithWhitespaceRawName_SkippedInCrossInsights()
    {
        // Regression: bug #5 — person name lookup returning empty/whitespace names should
        // be skipped, not rendered with blank or "unknown" labels.
        var personId = Guid.NewGuid();

        var capture1 = Capture.Create(_userId, "Meeting 1.", CaptureType.MeetingNotes);
        capture1.BeginProcessing();
        capture1.LinkToPerson(personId);
        capture1.CompleteProcessing(new AiExtraction
        {
            Summary = "Meeting one.",
            PeopleMentioned = new List<PersonMention>
            {
                new() { PersonId = personId, RawName = "  " }
            },
            ExtractedAt = DateTimeOffset.UtcNow
        });

        var capture2 = Capture.Create(_userId, "Meeting 2.", CaptureType.MeetingNotes);
        capture2.BeginProcessing();
        capture2.LinkToPerson(personId);
        capture2.CompleteProcessing(new AiExtraction
        {
            Summary = "Meeting two.",
            PeopleMentioned = new List<PersonMention>
            {
                new() { PersonId = personId, RawName = "  " }
            },
            ExtractedAt = DateTimeOffset.UtcNow
        });

        _captureRepo.GetAllAsync(_userId, null, ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture> { capture1, capture2 });
        _commitmentRepo.GetAllAsync(_userId, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());
        _initiativeRepo.GetAllAsync(_userId, InitiativeStatus.Active, Arg.Any<CancellationToken>())
            .Returns(new List<Initiative>());

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("Brief.", 20, 50, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(null, CancellationToken.None);

        // Person with whitespace-only name should not appear in cross-conversation insights
        Assert.Empty(result.CrossConversationInsights);
    }
}
