using MentalMetal.Application.MyQueue;
using MentalMetal.Application.MyQueue.Contracts;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Delegations;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace MentalMetal.Application.Tests.MyQueue;

public class GetMyQueueHandlerTests
{
    private readonly ICommitmentRepository _commitments = Substitute.For<ICommitmentRepository>();
    private readonly IDelegationRepository _delegations = Substitute.For<IDelegationRepository>();
    private readonly ICaptureRepository _captures = Substitute.For<ICaptureRepository>();
    private readonly IPersonRepository _people = Substitute.For<IPersonRepository>();
    private readonly IInitiativeRepository _initiatives = Substitute.For<IInitiativeRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly QueuePrioritizationService _scoring = new();
    private readonly MyQueueOptions _options = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _personId = Guid.NewGuid();
    // Use real UtcNow so domain-created aggregates (whose CreatedAt/CapturedAt are
    // DateTimeOffset.UtcNow) relate consistently to the handler's time provider.
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
    private readonly FakeTimeProvider _time;

    private readonly GetMyQueueHandler _handler;

    public GetMyQueueHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _time = new FakeTimeProvider(_now);
        _handler = new GetMyQueueHandler(
            _commitments, _delegations, _captures, _people, _initiatives,
            _currentUser, _scoring, Options.Create(_options), _time);

        // Default: no rows everywhere.
        _commitments.GetAllAsync(
            _userId, Arg.Any<CommitmentDirection?>(), Arg.Any<CommitmentStatus?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Commitment>());
        _delegations.GetAllAsync(
            _userId, Arg.Any<DelegationStatus?>(), Arg.Any<Priority?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Delegation>());
        _captures.GetCloseOutQueueAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Capture>());
        _people.GetByIdsAsync(_userId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Person>());
        _initiatives.GetByIdsAsync(_userId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Initiative>());
    }

    private static GetMyQueueQuery DefaultQuery(
        QueueScope scope = QueueScope.All,
        List<QueueItemType>? types = null,
        Guid? personId = null,
        Guid? initiativeId = null) =>
        new(scope, types ?? new List<QueueItemType>(), personId, initiativeId);

    [Fact]
    public async Task EmptyQueue_ReturnsZeroedCounts()
    {
        var result = await _handler.HandleAsync(DefaultQuery(), default);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Counts.Total);
        Assert.Equal(0, result.Counts.Overdue);
        Assert.Equal(0, result.Counts.DueSoon);
        Assert.Equal(0, result.Counts.StaleCaptures);
        Assert.Equal(0, result.Counts.StaleDelegations);
    }

    [Fact]
    public async Task MixedQueue_ReturnsAllThreeTypes()
    {
        var today = DateOnly.FromDateTime(_now.UtcDateTime);

        // Overdue commitment
        var commitment = Commitment.Create(_userId, "ship spec", CommitmentDirection.MineToThem, _personId, today.AddDays(-2));

        // Stale in-progress delegation
        var delegation = Delegation.Create(_userId, "chase vendor", _personId);
        delegation.MarkInProgress();
        _time.Advance(TimeSpan.FromDays(10));

        // Raw capture from 5 days ago (captured when time was "now"; we'll advance clock)
        var capture = Capture.Create(_userId, "thought", CaptureType.QuickNote);

        _commitments.GetAllAsync(_userId, null, CommitmentStatus.Open, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { commitment });
        _delegations.GetAllAsync(_userId, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { delegation });
        _captures.GetCloseOutQueueAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new[] { capture });

        var result = await _handler.HandleAsync(DefaultQuery(), default);

        Assert.Equal(3, result.Items.Count);
        Assert.Contains(result.Items, i => i.ItemType == QueueItemType.Commitment);
        Assert.Contains(result.Items, i => i.ItemType == QueueItemType.Delegation);
        Assert.Contains(result.Items, i => i.ItemType == QueueItemType.Capture);
        Assert.All(result.Items, i => Assert.True(i.PriorityScore > 0));
    }

    [Fact]
    public async Task FilterByItemType_Commitment_ReturnsOnlyCommitments()
    {
        var today = DateOnly.FromDateTime(_now.UtcDateTime);
        var c = Commitment.Create(_userId, "c", CommitmentDirection.MineToThem, _personId, today.AddDays(-1));
        var d = Delegation.Create(_userId, "d", _personId, priority: Priority.Urgent);

        _commitments.GetAllAsync(_userId, null, CommitmentStatus.Open, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { c });
        _delegations.GetAllAsync(_userId, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { d });

        var result = await _handler.HandleAsync(
            DefaultQuery(types: new List<QueueItemType> { QueueItemType.Commitment }), default);

        Assert.Single(result.Items);
        Assert.Equal(QueueItemType.Commitment, result.Items[0].ItemType);
    }

    [Fact]
    public async Task ScopeOverdue_ExcludesFutureDueItems()
    {
        var today = DateOnly.FromDateTime(_now.UtcDateTime);
        var overdue = Commitment.Create(_userId, "over", CommitmentDirection.MineToThem, _personId, today.AddDays(-1));
        var dueSoon = Commitment.Create(_userId, "soon", CommitmentDirection.MineToThem, _personId, today.AddDays(3));

        _commitments.GetAllAsync(_userId, null, CommitmentStatus.Open, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { overdue, dueSoon });

        var result = await _handler.HandleAsync(DefaultQuery(scope: QueueScope.Overdue), default);

        Assert.Single(result.Items);
        Assert.Equal(overdue.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task ScopeThisWeek_ExcludesDueInTenDays()
    {
        var today = DateOnly.FromDateTime(_now.UtcDateTime);
        var dueIn2 = Commitment.Create(_userId, "a", CommitmentDirection.MineToThem, _personId, today.AddDays(2));
        var dueIn10 = Commitment.Create(_userId, "b", CommitmentDirection.MineToThem, _personId, today.AddDays(10));

        _commitments.GetAllAsync(_userId, null, CommitmentStatus.Open, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { dueIn2, dueIn10 });

        var result = await _handler.HandleAsync(DefaultQuery(scope: QueueScope.ThisWeek), default);

        Assert.Single(result.Items);
        Assert.Equal(dueIn2.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task SuggestDelegate_TrueWhenUserHasDelegationToPerson()
    {
        var today = DateOnly.FromDateTime(_now.UtcDateTime);
        var commitment = Commitment.Create(_userId, "ask sarah", CommitmentDirection.MineToThem, _personId, today.AddDays(-1));
        var establishedDelegation = Delegation.Create(_userId, "previous", _personId);

        _commitments.GetAllAsync(_userId, null, CommitmentStatus.Open, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { commitment });
        _delegations.GetAllAsync(_userId, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { establishedDelegation });

        var result = await _handler.HandleAsync(DefaultQuery(), default);

        var commitmentItem = Assert.Single(result.Items, i => i.ItemType == QueueItemType.Commitment);
        Assert.True(commitmentItem.SuggestDelegate);
    }

    [Fact]
    public async Task SuggestDelegate_FalseWhenNoRelationship()
    {
        var today = DateOnly.FromDateTime(_now.UtcDateTime);
        var commitment = Commitment.Create(_userId, "ask alex", CommitmentDirection.MineToThem, _personId, today.AddDays(-1));

        _commitments.GetAllAsync(_userId, null, CommitmentStatus.Open, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { commitment });

        var result = await _handler.HandleAsync(DefaultQuery(), default);

        var commitmentItem = Assert.Single(result.Items);
        Assert.False(commitmentItem.SuggestDelegate);
    }

    [Fact]
    public async Task SuggestDelegate_FalseForTheirsToMeCommitment()
    {
        var today = DateOnly.FromDateTime(_now.UtcDateTime);
        var commitment = Commitment.Create(_userId, "they owe me", CommitmentDirection.TheirsToMe, _personId, today.AddDays(-1));
        var delegation = Delegation.Create(_userId, "prev", _personId);

        _commitments.GetAllAsync(_userId, null, CommitmentStatus.Open, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { commitment });
        _delegations.GetAllAsync(_userId, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { delegation });

        var result = await _handler.HandleAsync(DefaultQuery(), default);

        var item = Assert.Single(result.Items, i => i.ItemType == QueueItemType.Commitment);
        Assert.False(item.SuggestDelegate);
    }

    [Fact]
    public async Task DelegationItem_SuggestDelegateAlwaysFalse()
    {
        var d = Delegation.Create(_userId, "d", _personId, priority: Priority.Urgent);
        _delegations.GetAllAsync(_userId, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { d });

        var result = await _handler.HandleAsync(DefaultQuery(), default);
        var item = Assert.Single(result.Items);
        Assert.False(item.SuggestDelegate);
    }

    [Fact]
    public async Task OrderedByPriorityScoreDescending()
    {
        var today = DateOnly.FromDateTime(_now.UtcDateTime);
        var urgentOverdue = Commitment.Create(_userId, "urgent", CommitmentDirection.MineToThem, _personId, today.AddDays(-10));
        var mildlyDueSoon = Commitment.Create(_userId, "mild", CommitmentDirection.MineToThem, _personId, today.AddDays(5));

        _commitments.GetAllAsync(_userId, null, CommitmentStatus.Open, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { mildlyDueSoon, urgentOverdue });

        var result = await _handler.HandleAsync(DefaultQuery(), default);

        Assert.Equal(urgentOverdue.Id, result.Items[0].Id);
        Assert.Equal(mildlyDueSoon.Id, result.Items[1].Id);
    }

    [Fact]
    public async Task CountsReflectFilteredItems()
    {
        var today = DateOnly.FromDateTime(_now.UtcDateTime);
        var c1 = Commitment.Create(_userId, "a", CommitmentDirection.MineToThem, _personId, today.AddDays(-1));
        var c2 = Commitment.Create(_userId, "b", CommitmentDirection.MineToThem, _personId, today.AddDays(-2));
        var cap = Capture.Create(_userId, "cap", CaptureType.QuickNote);

        _commitments.GetAllAsync(_userId, null, CommitmentStatus.Open, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { c1, c2 });
        _captures.GetCloseOutQueueAsync(_userId, Arg.Any<CancellationToken>()).Returns(new[] { cap });

        var result = await _handler.HandleAsync(
            DefaultQuery(types: new List<QueueItemType> { QueueItemType.Commitment }), default);

        Assert.Equal(2, result.Counts.Total);
        Assert.Equal(0, result.Counts.StaleCaptures);
        Assert.Equal(2, result.Counts.Overdue);
    }

    [Fact]
    public async Task FilterByPersonId_PassesThroughToRepositories()
    {
        var other = Guid.NewGuid();
        await _handler.HandleAsync(DefaultQuery(personId: other), default);

        await _commitments.Received(1).GetAllAsync(
            _userId, null, CommitmentStatus.Open, other, null, null, Arg.Any<CancellationToken>());
        await _delegations.Received(1).GetAllAsync(
            _userId, null, null, other, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterByInitiativeId_PassesThroughToRepositories()
    {
        var initiativeId = Guid.NewGuid();
        await _handler.HandleAsync(DefaultQuery(initiativeId: initiativeId), default);

        await _commitments.Received(1).GetAllAsync(
            _userId, null, CommitmentStatus.Open, null, initiativeId, null, Arg.Any<CancellationToken>());
        await _delegations.Received(1).GetAllAsync(
            _userId, null, null, null, initiativeId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CandidateFetchCap_Enforced()
    {
        // Generate 300 commitments, all overdue — only CandidateFetchLimit (200) should be kept.
        var today = DateOnly.FromDateTime(_now.UtcDateTime);
        var many = Enumerable.Range(1, 300)
            .Select(i => Commitment.Create(_userId, $"c{i}", CommitmentDirection.MineToThem, _personId, today.AddDays(-1)))
            .ToArray();

        _commitments.GetAllAsync(_userId, null, CommitmentStatus.Open, null, null, null, Arg.Any<CancellationToken>())
            .Returns(many);

        var result = await _handler.HandleAsync(DefaultQuery(), default);

        Assert.Equal(_options.CandidateFetchLimit, result.Items.Count);
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _current;
        public FakeTimeProvider(DateTimeOffset start) => _current = start;
        public override DateTimeOffset GetUtcNow() => _current;
        public void Advance(TimeSpan by) => _current = _current.Add(by);
    }
}
