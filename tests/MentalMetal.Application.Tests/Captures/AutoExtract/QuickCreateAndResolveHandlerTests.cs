using MentalMetal.Application.Captures;
using MentalMetal.Application.Captures.AutoExtract;
using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Captures.AutoExtract;

public class QuickCreateAndResolveHandlerTests
{
    private readonly ICaptureRepository _captureRepo = Substitute.For<ICaptureRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly ICommitmentRepository _commitmentRepo = Substitute.For<ICommitmentRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly QuickCreateAndResolveHandler _sut;

    public QuickCreateAndResolveHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _sut = new QuickCreateAndResolveHandler(
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
    public async Task HandleAsync_CreatesPersonAndSpawnsCommitments()
    {
        var capture = CreateProcessedCaptureWithExtraction(
            [new PersonMention { RawName = "Sarah", Context = "discussed project" }],
            [new ExtractedCommitment
            {
                Description = "Send the API spec",
                Direction = CommitmentDirection.TheirsToMe,
                PersonRawName = "Sarah",
                PersonId = null,
                Confidence = CommitmentConfidence.High,
                SpawnedCommitmentId = null
            }]);

        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);
        _personRepo.ExistsByNameAsync(_userId, "Sarah Chen", null, Arg.Any<CancellationToken>()).Returns(false);
        _personRepo.AliasExistsForOtherPersonAsync(_userId, "Sarah", Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var request = new QuickCreateAndResolveRequest("Sarah", "Sarah Chen", PersonType.Stakeholder);
        var result = await _sut.HandleAsync(capture.Id, request, CancellationToken.None);

        await _personRepo.Received(1).AddAsync(Arg.Is<Person>(p => p.Name == "Sarah Chen" && p.Type == PersonType.Stakeholder), Arg.Any<CancellationToken>());
        await _commitmentRepo.Received(1).AddAsync(Arg.Any<Commitment>(), Arg.Any<CancellationToken>());
        Assert.NotEmpty(result.SpawnedCommitmentIds);
        Assert.NotNull(result.AiExtraction!.Commitments[0].SpawnedCommitmentId);
        Assert.NotNull(result.AiExtraction.PeopleMentioned[0].PersonId);
    }

    [Fact]
    public async Task HandleAsync_DuplicatePersonName_ThrowsDuplicatePersonNameException()
    {
        var capture = CreateProcessedCaptureWithExtraction(
            [new PersonMention { RawName = "Alice", Context = null }],
            []);

        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);
        _personRepo.ExistsByNameAsync(_userId, "Alice Smith", null, Arg.Any<CancellationToken>()).Returns(true);

        var request = new QuickCreateAndResolveRequest("Alice", "Alice Smith", PersonType.Peer);

        await Assert.ThrowsAsync<DuplicatePersonNameException>(
            () => _sut.HandleAsync(capture.Id, request, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_SameNameAsRawName_SkipsAlias()
    {
        var capture = CreateProcessedCaptureWithExtraction(
            [new PersonMention { RawName = "Sarah Chen", Context = null }],
            []);

        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);
        _personRepo.ExistsByNameAsync(_userId, "Sarah Chen", null, Arg.Any<CancellationToken>()).Returns(false);

        var request = new QuickCreateAndResolveRequest("Sarah Chen", "Sarah Chen", PersonType.Stakeholder);
        await _sut.HandleAsync(capture.Id, request, CancellationToken.None);

        // Person should be added but alias should NOT be checked/added (name matches raw name)
        await _personRepo.Received(1).AddAsync(
            Arg.Is<Person>(p => p.Aliases.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoExtraction_Throws()
    {
        var capture = Capture.Create(_userId, "Test", CaptureType.QuickNote);
        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);

        var request = new QuickCreateAndResolveRequest("Sarah", "Sarah Chen", PersonType.Stakeholder);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(capture.Id, request, CancellationToken.None));
        Assert.Contains("no AI extraction", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_UnknownRawName_Throws()
    {
        var capture = CreateProcessedCaptureWithExtraction(
            [new PersonMention { RawName = "Alice", Context = null }],
            []);

        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);
        _personRepo.ExistsByNameAsync(_userId, "Bob", null, Arg.Any<CancellationToken>()).Returns(false);

        var request = new QuickCreateAndResolveRequest("Bob", "Bob", PersonType.Peer);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(capture.Id, request, CancellationToken.None));
        Assert.Contains("found in extraction", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_AlreadyResolvedMention_Throws()
    {
        var capture = CreateProcessedCaptureWithExtraction(
            [new PersonMention { RawName = "Alice", PersonId = Guid.NewGuid(), Context = null }],
            []);

        _captureRepo.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);

        var request = new QuickCreateAndResolveRequest("Alice", "Alice Smith", PersonType.Peer);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(capture.Id, request, CancellationToken.None));
        Assert.Contains("already resolved", ex.Message);
    }
}
