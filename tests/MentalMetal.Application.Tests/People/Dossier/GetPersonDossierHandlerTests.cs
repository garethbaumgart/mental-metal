using MentalMetal.Application.Common.Ai;
using MentalMetal.Application.People.Dossier;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.People.Dossier;

public class GetPersonDossierHandlerTests
{
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly ICaptureRepository _captureRepo = Substitute.For<ICaptureRepository>();
    private readonly ICommitmentRepository _commitmentRepo = Substitute.For<ICommitmentRepository>();
    private readonly IAiCompletionService _aiService = Substitute.For<IAiCompletionService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly GetPersonDossierHandler _sut;

    public GetPersonDossierHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _sut = new GetPersonDossierHandler(
            _personRepo, _captureRepo, _commitmentRepo, _aiService, _currentUser);
    }

    [Fact]
    public async Task HandleAsync_ReturnsDossierWithSynthesis()
    {
        var person = Person.Create(_userId, "Alice", PersonType.Peer);
        _personRepo.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);
        _captureRepo.GetAllAsync(_userId, null, ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture>());
        _commitmentRepo.GetAllAsync(_userId, null, CommitmentStatus.Open, person.Id, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("Alice is a valued peer.", 50, 100, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(person.Id, "default", 20, CancellationToken.None);

        Assert.Equal(person.Id, result.PersonId);
        Assert.Equal("Alice", result.PersonName);
        Assert.Equal("Alice is a valued peer.", result.Synthesis);
        Assert.Empty(result.OpenCommitments);
        Assert.Empty(result.TranscriptMentions);
    }

    [Fact]
    public async Task HandleAsync_PrepMode_UsesCorrectPrompt()
    {
        var person = Person.Create(_userId, "Bob", PersonType.DirectReport);
        _personRepo.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);
        _captureRepo.GetAllAsync(_userId, null, ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture>());
        _commitmentRepo.GetAllAsync(_userId, null, CommitmentStatus.Open, person.Id, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("Prep brief for Bob.", 50, 100, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(person.Id, "prep", 20, CancellationToken.None);

        Assert.Equal("Prep brief for Bob.", result.Synthesis);

        // Verify the AI was called with a prompt containing pre-meeting prep language
        await _aiService.Received(1).CompleteAsync(
            Arg.Is<AiCompletionRequest>(r => r.SystemPrompt.Contains("pre-meeting")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithLinkedCaptures_IncludesTranscriptMentions()
    {
        var person = Person.Create(_userId, "Charlie", PersonType.Stakeholder);
        _personRepo.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);

        var capture = Capture.Create(_userId, "Meeting with Charlie about the project.", CaptureType.MeetingNotes, title: "Project Review");
        capture.LinkToPerson(person.Id);
        capture.BeginProcessing();
        capture.CompleteProcessing(new AiExtraction
        {
            Summary = "Project review meeting.",
            PeopleMentioned = new List<PersonMention>
            {
                new() { RawName = "Charlie", PersonId = person.Id, Context = "Led the review session" }
            },
            ExtractedAt = DateTimeOffset.UtcNow
        });

        _captureRepo.GetAllAsync(_userId, null, ProcessingStatus.Processed, Arg.Any<CancellationToken>())
            .Returns(new List<Capture> { capture });
        _commitmentRepo.GetAllAsync(_userId, null, CommitmentStatus.Open, person.Id, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());

        _aiService.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("Charlie led the project review.", 100, 200, "test-model", AiProvider.Anthropic));

        var result = await _sut.HandleAsync(person.Id, "default", 20, CancellationToken.None);

        Assert.Single(result.TranscriptMentions);
        Assert.Equal("Project Review", result.TranscriptMentions[0].CaptureTitle);
        Assert.Equal("Led the review session", result.TranscriptMentions[0].MentionContext);
    }

    [Fact]
    public async Task HandleAsync_PersonNotFound_ThrowsNotFoundException()
    {
        _personRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Person?)null);

        await Assert.ThrowsAsync<MentalMetal.Domain.Common.NotFoundException>(
            () => _sut.HandleAsync(Guid.NewGuid(), "default", 20, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_PersonBelongsToDifferentUser_ThrowsNotFoundException()
    {
        var otherUserId = Guid.NewGuid();
        var person = Person.Create(otherUserId, "Eve", PersonType.External);
        _personRepo.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);

        await Assert.ThrowsAsync<MentalMetal.Domain.Common.NotFoundException>(
            () => _sut.HandleAsync(person.Id, "default", 20, CancellationToken.None));
    }
}
