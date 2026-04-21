using MentalMetal.Application.Captures;
using MentalMetal.Application.Captures.AutoExtract;
using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Captures.AutoExtract;

public class ResolvePersonMentionHandlerTests
{
    private readonly ICaptureRepository _captureRepo = Substitute.For<ICaptureRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly ICommitmentRepository _commitmentRepo = Substitute.For<ICommitmentRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly ResolvePersonMentionHandler _sut;

    public ResolvePersonMentionHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _sut = new ResolvePersonMentionHandler(
            _captureRepo, _personRepo, _commitmentRepo, _currentUser, _unitOfWork);
    }

    private Capture CreateProcessedCaptureWithExtraction(
        IReadOnlyList<PersonMention> people,
        IReadOnlyList<ExtractedCommitment> commitments)
    {
        var capture = Capture.Create(_userId, "Test content", CaptureType.QuickNote);
        capture.BeginProcessing();
        capture.CompleteProcessing(new AiExtraction
        {
            Summary = "Test",
            PeopleMentioned = people,
            Commitments = commitments,
            Decisions = [],
            Risks = [],
            InitiativeTags = [],
            ExtractedAt = DateTimeOffset.UtcNow
        });
        return capture;
    }

    [Fact]
    public async Task HandleAsync_SpawnsSkippedHighConfidenceCommitments()
    {
        var person = Person.Create(_userId, "Sarah Chen", PersonType.Stakeholder);
        var capture = CreateProcessedCaptureWithExtraction(
            [new PersonMention { RawName = "Sarah", Context = "mentioned" }],
            [new ExtractedCommitment
            {
                Description = "Send the report",
                Direction = CommitmentDirection.MineToThem,
                PersonRawName = "Sarah",
                PersonId = null,
                Confidence = CommitmentConfidence.High,
                SpawnedCommitmentId = null
            }]);

        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);
        _personRepo.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);

        var request = new ResolvePersonMentionRequest("Sarah", person.Id);
        var result = await _sut.HandleAsync(capture.Id, request, CancellationToken.None);

        await _commitmentRepo.Received(1).AddAsync(Arg.Any<Commitment>(), Arg.Any<CancellationToken>());
        Assert.NotEmpty(result.SpawnedCommitmentIds);
        Assert.NotNull(result.AiExtraction!.Commitments[0].SpawnedCommitmentId);
        Assert.Equal(person.Id, result.AiExtraction.Commitments[0].PersonId);
    }

    [Fact]
    public async Task HandleAsync_DoesNotSpawnLowConfidenceCommitments()
    {
        var person = Person.Create(_userId, "Mike Jones", PersonType.Peer);
        var capture = CreateProcessedCaptureWithExtraction(
            [new PersonMention { RawName = "Mike", Context = null }],
            [new ExtractedCommitment
            {
                Description = "Maybe look into this",
                Direction = CommitmentDirection.TheirsToMe,
                PersonRawName = "Mike",
                PersonId = null,
                Confidence = CommitmentConfidence.Low,
                SpawnedCommitmentId = null
            }]);

        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);
        _personRepo.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);

        var request = new ResolvePersonMentionRequest("Mike", person.Id);
        var result = await _sut.HandleAsync(capture.Id, request, CancellationToken.None);

        await _commitmentRepo.DidNotReceive().AddAsync(Arg.Any<Commitment>(), Arg.Any<CancellationToken>());
        Assert.Empty(result.SpawnedCommitmentIds);
        Assert.Null(result.AiExtraction!.Commitments[0].SpawnedCommitmentId);
        // PersonId should still be updated
        Assert.Equal(person.Id, result.AiExtraction.Commitments[0].PersonId);
    }

    [Fact]
    public async Task HandleAsync_DoesNotDuplicateAlreadySpawnedCommitments()
    {
        var person = Person.Create(_userId, "Alice", PersonType.DirectReport);
        var existingCommitmentId = Guid.NewGuid();
        var capture = CreateProcessedCaptureWithExtraction(
            [new PersonMention { RawName = "Alice", Context = null }],
            [new ExtractedCommitment
            {
                Description = "Already spawned commitment",
                Direction = CommitmentDirection.MineToThem,
                PersonRawName = "Alice",
                PersonId = null,
                Confidence = CommitmentConfidence.High,
                SpawnedCommitmentId = existingCommitmentId
            }]);

        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);
        _personRepo.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);

        var request = new ResolvePersonMentionRequest("Alice", person.Id);
        var result = await _sut.HandleAsync(capture.Id, request, CancellationToken.None);

        await _commitmentRepo.DidNotReceive().AddAsync(Arg.Any<Commitment>(), Arg.Any<CancellationToken>());
        Assert.Equal(existingCommitmentId, result.AiExtraction!.Commitments[0].SpawnedCommitmentId);
    }

    [Fact]
    public async Task HandleAsync_ResolvedWithNoCommitments_NoSpawning()
    {
        var person = Person.Create(_userId, "Dave", PersonType.Peer);
        var capture = CreateProcessedCaptureWithExtraction(
            [new PersonMention { RawName = "Dave", Context = null }],
            []);

        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);
        _personRepo.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);

        var request = new ResolvePersonMentionRequest("Dave", person.Id);
        var result = await _sut.HandleAsync(capture.Id, request, CancellationToken.None);

        await _commitmentRepo.DidNotReceive().AddAsync(Arg.Any<Commitment>(), Arg.Any<CancellationToken>());
        Assert.Equal(person.Id, result.AiExtraction!.PeopleMentioned[0].PersonId);
    }

    [Fact]
    public async Task HandleAsync_SpawnsMediumConfidenceCommitments()
    {
        var person = Person.Create(_userId, "Eve", PersonType.Stakeholder);
        var capture = CreateProcessedCaptureWithExtraction(
            [new PersonMention { RawName = "Eve", Context = null }],
            [new ExtractedCommitment
            {
                Description = "Follow up on proposal",
                Direction = CommitmentDirection.TheirsToMe,
                PersonRawName = "Eve",
                PersonId = null,
                Confidence = CommitmentConfidence.Medium,
                SpawnedCommitmentId = null
            }]);

        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);
        _personRepo.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);

        var request = new ResolvePersonMentionRequest("Eve", person.Id);
        var result = await _sut.HandleAsync(capture.Id, request, CancellationToken.None);

        await _commitmentRepo.Received(1).AddAsync(Arg.Any<Commitment>(), Arg.Any<CancellationToken>());
        Assert.NotEmpty(result.SpawnedCommitmentIds);
    }

    [Fact]
    public async Task HandleAsync_AlreadyResolvedMention_Throws()
    {
        var person = Person.Create(_userId, "Alice", PersonType.DirectReport);
        var capture = CreateProcessedCaptureWithExtraction(
            [new PersonMention { RawName = "Alice", PersonId = Guid.NewGuid(), Context = null }],
            []);

        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);
        _personRepo.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);

        var request = new ResolvePersonMentionRequest("Alice", person.Id);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(capture.Id, request, CancellationToken.None));
        Assert.Contains("already resolved", ex.Message);
    }
}
