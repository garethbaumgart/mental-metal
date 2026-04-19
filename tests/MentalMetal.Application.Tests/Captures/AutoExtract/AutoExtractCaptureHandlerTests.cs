using MentalMetal.Application.Captures.AutoExtract;
using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace MentalMetal.Application.Tests.Captures.AutoExtract;

public class AutoExtractCaptureHandlerTests
{
    private readonly ICaptureRepository _captureRepo = Substitute.For<ICaptureRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly IInitiativeRepository _initiativeRepo = Substitute.For<IInitiativeRepository>();
    private readonly ICommitmentRepository _commitmentRepo = Substitute.For<ICommitmentRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IAiCompletionService _aiService = Substitute.For<IAiCompletionService>();
    private readonly ITasteBudgetService _tasteBudget = Substitute.For<ITasteBudgetService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<AutoExtractCaptureHandler> _logger = NullLogger<AutoExtractCaptureHandler>.Instance;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly AutoExtractCaptureHandler _sut;

    public AutoExtractCaptureHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _tasteBudget.IsEnabled.Returns(false);
        _personRepo.GetAllAsync(_userId, null, false, Arg.Any<CancellationToken>())
            .Returns(new List<Person>());
        _initiativeRepo.GetAllAsync(_userId, InitiativeStatus.Active, Arg.Any<CancellationToken>())
            .Returns(new List<Initiative>());
        _userRepo.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(User.Register("ext-auth-id", "test@example.com", "Test User", null));

        _sut = new AutoExtractCaptureHandler(
            _captureRepo, _personRepo, _initiativeRepo, _commitmentRepo,
            _userRepo, _aiService, _tasteBudget, _currentUser,
            new NameResolutionService(), new InitiativeTaggingService(),
            _unitOfWork, _logger);
    }

    private Capture CreateRawCapture(string content = "Test meeting notes")
    {
        return Capture.Create(_userId, content, CaptureType.MeetingNotes);
    }

    [Fact]
    public async Task HandleAsync_SuccessfulExtraction_SetsProcessedStatus()
    {
        var capture = CreateRawCapture();
        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>())
            .Returns(capture);

        var aiJson = """
        {
          "summary": "A brief meeting about project updates.",
          "people_mentioned": [],
          "commitments": [],
          "decisions": ["Approved the budget"],
          "risks": ["Timeline is tight"],
          "initiative_tags": []
        }
        """;

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult(aiJson, 100, 200, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(capture.Id, CancellationToken.None);

        Assert.Equal(ProcessingStatus.Processed, result.ProcessingStatus);
        Assert.NotNull(result.AiExtraction);
        Assert.Equal("A brief meeting about project updates.", result.AiExtraction!.Summary);
        Assert.Single(result.AiExtraction.Decisions);
        Assert.Single(result.AiExtraction.Risks);
    }

    [Fact]
    public async Task HandleAsync_AiFailure_SetsFailedStatus()
    {
        var capture = CreateRawCapture();

        // The handler calls GetByIdAsync twice: once at the start (Raw), once after
        // failure (returns a fresh Processing capture simulating what was persisted).
        var callCount = 0;
        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount == 1)
                    return capture;
                var fresh = Capture.Create(_userId, "Test meeting notes", CaptureType.MeetingNotes);
                fresh.BeginProcessing();
                return fresh;
            });

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns<AiCompletionResult>(_ => throw new AiProviderException(AiProvider.Anthropic, 429, "API rate limited"));

        var result = await _sut.HandleAsync(capture.Id, CancellationToken.None);

        Assert.Equal(ProcessingStatus.Failed, result.ProcessingStatus);
        Assert.Contains("API rate limited", result.FailureReason);
    }

    [Fact]
    public async Task HandleAsync_WithPeopleResolution_LinksResolvedPeople()
    {
        var capture = CreateRawCapture();
        var alice = Person.Create(_userId, "Alice Smith", PersonType.Peer);

        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>())
            .Returns(capture);
        _personRepo.GetAllAsync(_userId, null, false, Arg.Any<CancellationToken>())
            .Returns(new List<Person> { alice });

        var aiJson = """
        {
          "summary": "Met with Alice about the project.",
          "people_mentioned": [{"raw_name": "Alice Smith", "context": "discussed project"}],
          "commitments": [],
          "decisions": [],
          "risks": [],
          "initiative_tags": []
        }
        """;

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult(aiJson, 100, 200, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(capture.Id, CancellationToken.None);

        Assert.Contains(alice.Id, result.LinkedPersonIds);
        Assert.Equal(alice.Id, result.AiExtraction!.PeopleMentioned[0].PersonId);
    }

    [Fact]
    public async Task HandleAsync_HighConfidenceCommitment_SpawnsCommitmentEntity()
    {
        var capture = CreateRawCapture();
        var bob = Person.Create(_userId, "Bob Builder", PersonType.DirectReport);

        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>())
            .Returns(capture);
        _personRepo.GetAllAsync(_userId, null, false, Arg.Any<CancellationToken>())
            .Returns(new List<Person> { bob });

        var aiJson = """
        {
          "summary": "Planning session.",
          "people_mentioned": [{"raw_name": "Bob Builder", "context": null}],
          "commitments": [{
            "description": "Send the updated report",
            "direction": "MineToThem",
            "person_raw_name": "Bob Builder",
            "due_date": "2026-04-25",
            "confidence": "High"
          }],
          "decisions": [],
          "risks": [],
          "initiative_tags": []
        }
        """;

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult(aiJson, 100, 200, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(capture.Id, CancellationToken.None);

        // Commitment should have been spawned
        await _commitmentRepo.Received(1).AddAsync(Arg.Any<Commitment>(), Arg.Any<CancellationToken>());
        Assert.NotEmpty(result.SpawnedCommitmentIds);
        Assert.NotNull(result.AiExtraction!.Commitments[0].SpawnedCommitmentId);
    }

    [Fact]
    public async Task HandleAsync_LowConfidenceCommitment_DoesNotSpawnEntity()
    {
        var capture = CreateRawCapture();
        var carol = Person.Create(_userId, "Carol Danvers", PersonType.Peer);

        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>())
            .Returns(capture);
        _personRepo.GetAllAsync(_userId, null, false, Arg.Any<CancellationToken>())
            .Returns(new List<Person> { carol });

        var aiJson = """
        {
          "summary": "Quick chat.",
          "people_mentioned": [{"raw_name": "Carol Danvers", "context": null}],
          "commitments": [{
            "description": "Maybe look into the issue",
            "direction": "MineToThem",
            "person_raw_name": "Carol Danvers",
            "due_date": null,
            "confidence": "Low"
          }],
          "decisions": [],
          "risks": [],
          "initiative_tags": []
        }
        """;

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult(aiJson, 100, 200, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(capture.Id, CancellationToken.None);

        // Low confidence — no commitment entity spawned
        await _commitmentRepo.DidNotReceive().AddAsync(Arg.Any<Commitment>(), Arg.Any<CancellationToken>());
        Assert.Empty(result.SpawnedCommitmentIds);
        Assert.Null(result.AiExtraction!.Commitments[0].SpawnedCommitmentId);
    }

    [Fact]
    public async Task HandleAsync_MediumConfidenceWithResolvedPerson_SpawnsCommitment()
    {
        var capture = CreateRawCapture();
        var dave = Person.Create(_userId, "Dave Wilson", PersonType.Stakeholder);

        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>())
            .Returns(capture);
        _personRepo.GetAllAsync(_userId, null, false, Arg.Any<CancellationToken>())
            .Returns(new List<Person> { dave });

        var aiJson = """
        {
          "summary": "Status update.",
          "people_mentioned": [{"raw_name": "Dave Wilson", "context": null}],
          "commitments": [{
            "description": "Follow up on the proposal",
            "direction": "TheirsToMe",
            "person_raw_name": "Dave Wilson",
            "due_date": null,
            "confidence": "Medium"
          }],
          "decisions": [],
          "risks": [],
          "initiative_tags": []
        }
        """;

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult(aiJson, 100, 200, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(capture.Id, CancellationToken.None);

        await _commitmentRepo.Received(1).AddAsync(Arg.Any<Commitment>(), Arg.Any<CancellationToken>());
        Assert.NotEmpty(result.SpawnedCommitmentIds);
    }

    [Fact]
    public async Task HandleAsync_CommitmentWithUnresolvedPerson_DoesNotSpawn()
    {
        var capture = CreateRawCapture();

        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>())
            .Returns(capture);

        var aiJson = """
        {
          "summary": "Quick sync.",
          "people_mentioned": [{"raw_name": "Unknown Person", "context": null}],
          "commitments": [{
            "description": "Send the document",
            "direction": "MineToThem",
            "person_raw_name": "Unknown Person",
            "due_date": null,
            "confidence": "High"
          }],
          "decisions": [],
          "risks": [],
          "initiative_tags": []
        }
        """;

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult(aiJson, 100, 200, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(capture.Id, CancellationToken.None);

        // Person is unresolved — cannot spawn commitment (PersonId is required)
        await _commitmentRepo.DidNotReceive().AddAsync(Arg.Any<Commitment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithInitiativeTagging_LinksResolvedInitiatives()
    {
        var capture = CreateRawCapture();
        var initiative = Initiative.Create(_userId, "Project Alpha");

        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>())
            .Returns(capture);
        _initiativeRepo.GetAllAsync(_userId, InitiativeStatus.Active, Arg.Any<CancellationToken>())
            .Returns(new List<Initiative> { initiative });

        var aiJson = """
        {
          "summary": "Discussion about Project Alpha.",
          "people_mentioned": [],
          "commitments": [],
          "decisions": [],
          "risks": [],
          "initiative_tags": [{"raw_name": "Project Alpha", "context": "main topic"}]
        }
        """;

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult(aiJson, 100, 200, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(capture.Id, CancellationToken.None);

        Assert.Contains(initiative.Id, result.LinkedInitiativeIds);
        Assert.Equal(initiative.Id, result.AiExtraction!.InitiativeTags[0].InitiativeId);
    }

    [Fact]
    public async Task HandleAsync_AiReturnsCodeFences_ParsesCorrectly()
    {
        var capture = CreateRawCapture();
        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>())
            .Returns(capture);

        var aiJson = """
        ```json
        {
          "summary": "Wrapped in code fences.",
          "people_mentioned": [],
          "commitments": [],
          "decisions": [],
          "risks": [],
          "initiative_tags": []
        }
        ```
        """;

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult(aiJson, 100, 200, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(capture.Id, CancellationToken.None);

        Assert.Equal(ProcessingStatus.Processed, result.ProcessingStatus);
        Assert.Equal("Wrapped in code fences.", result.AiExtraction!.Summary);
    }

    [Fact]
    public async Task HandleAsync_SystemPromptIncludesUserName()
    {
        var capture = CreateRawCapture();
        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>())
            .Returns(capture);

        var aiJson = """
        {
          "summary": "Quick sync.",
          "people_mentioned": [],
          "commitments": [],
          "decisions": [],
          "risks": [],
          "initiative_tags": []
        }
        """;

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult(aiJson, 100, 200, "test-model", AiProvider.Anthropic));

        await _sut.HandleAsync(capture.Id, CancellationToken.None);

        await _aiService.Received(1).CompleteAsync(
            Arg.Is<AiCompletionRequest>(r => r.SystemPrompt.Contains("Test User")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_SetsFailedStatus()
    {
        var capture = CreateRawCapture();

        var callCount = 0;
        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount == 1) return capture;
                var fresh = Capture.Create(_userId, "Test meeting notes", CaptureType.MeetingNotes);
                fresh.BeginProcessing();
                return fresh;
            });
        _userRepo.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _sut.HandleAsync(capture.Id, CancellationToken.None);

        Assert.Equal(ProcessingStatus.Failed, result.ProcessingStatus);
    }
}
