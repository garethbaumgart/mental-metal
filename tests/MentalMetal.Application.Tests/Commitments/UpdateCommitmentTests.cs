using MentalMetal.Application.Commitments;
using MentalMetal.Application.Common;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Commitments;

public class UpdateCommitmentTests
{
    private readonly ICommitmentRepository _commitmentRepo = Substitute.For<ICommitmentRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _personId = Guid.NewGuid();
    private readonly UpdateCommitmentHandler _sut;

    public UpdateCommitmentTests()
    {
        _currentUser.UserId.Returns(_userId);
        _sut = new UpdateCommitmentHandler(_commitmentRepo, _currentUser, _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_UpdateDirection_ChangesDirection()
    {
        var commitment = Commitment.Create(_userId, "Send report", CommitmentDirection.MineToThem, _personId);
        _commitmentRepo.GetByIdAsync(commitment.Id, Arg.Any<CancellationToken>())
            .Returns(commitment);

        var result = await _sut.HandleAsync(
            commitment.Id,
            new UpdateCommitmentRequest(Direction: CommitmentDirection.TheirsToMe),
            CancellationToken.None);

        Assert.Equal(CommitmentDirection.TheirsToMe, result.Direction);
    }

    [Fact]
    public async Task HandleAsync_UpdateDescription_ChangesDescription()
    {
        var commitment = Commitment.Create(_userId, "Old description", CommitmentDirection.MineToThem, _personId);
        _commitmentRepo.GetByIdAsync(commitment.Id, Arg.Any<CancellationToken>())
            .Returns(commitment);

        var result = await _sut.HandleAsync(
            commitment.Id,
            new UpdateCommitmentRequest(Description: "New description"),
            CancellationToken.None);

        Assert.Equal("New description", result.Description);
    }

    [Fact]
    public async Task HandleAsync_UpdateDueDate_ChangesDueDate()
    {
        var commitment = Commitment.Create(_userId, "Test", CommitmentDirection.MineToThem, _personId);
        _commitmentRepo.GetByIdAsync(commitment.Id, Arg.Any<CancellationToken>())
            .Returns(commitment);

        var dueDate = new DateOnly(2026, 5, 1);
        var result = await _sut.HandleAsync(
            commitment.Id,
            new UpdateCommitmentRequest(DueDate: dueDate),
            CancellationToken.None);

        Assert.Equal(dueDate, result.DueDate);
    }

    [Fact]
    public async Task HandleAsync_ClearDueDate_RemovesDueDate()
    {
        var commitment = Commitment.Create(_userId, "Test", CommitmentDirection.MineToThem, _personId,
            new DateOnly(2026, 5, 1));
        _commitmentRepo.GetByIdAsync(commitment.Id, Arg.Any<CancellationToken>())
            .Returns(commitment);

        var result = await _sut.HandleAsync(
            commitment.Id,
            new UpdateCommitmentRequest(ClearDueDate: true),
            CancellationToken.None);

        Assert.Null(result.DueDate);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ThrowsInvalidOperationException()
    {
        _commitmentRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Commitment?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(Guid.NewGuid(), new UpdateCommitmentRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_WrongUser_ThrowsInvalidOperationException()
    {
        var otherUserId = Guid.NewGuid();
        var commitment = Commitment.Create(otherUserId, "Test", CommitmentDirection.MineToThem, _personId);
        _commitmentRepo.GetByIdAsync(commitment.Id, Arg.Any<CancellationToken>())
            .Returns(commitment);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(commitment.Id, new UpdateCommitmentRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_EmptyDescription_ThrowsArgumentException()
    {
        var commitment = Commitment.Create(_userId, "Test", CommitmentDirection.MineToThem, _personId);
        _commitmentRepo.GetByIdAsync(commitment.Id, Arg.Any<CancellationToken>())
            .Returns(commitment);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.HandleAsync(commitment.Id, new UpdateCommitmentRequest(Description: ""), CancellationToken.None));
    }
}
