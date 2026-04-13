using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Application.Initiatives.Brief;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace MentalMetal.Application.Tests.Initiatives;

public class BriefMaintenanceServiceTests
{
    private readonly IInitiativeRepository _initiatives = Substitute.For<IInitiativeRepository>();
    private readonly ICaptureRepository _captures = Substitute.For<ICaptureRepository>();
    private readonly IPendingBriefUpdateRepository _pending = Substitute.For<IPendingBriefUpdateRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IAiCompletionService _ai = Substitute.For<IAiCompletionService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly BriefRefreshQueue _queue = new();
    private readonly BriefMaintenanceService _svc;

    private readonly Guid _userId = Guid.NewGuid();

    public BriefMaintenanceServiceTests()
    {
        _svc = new BriefMaintenanceService(_initiatives, _captures, _pending, _users, _ai, _uow, _queue, NullLogger<BriefMaintenanceService>.Instance);
    }

    private Initiative MakeInitiative()
    {
        var i = Initiative.Create(_userId, "Init");
        _initiatives.GetByIdAsync(i.Id, Arg.Any<CancellationToken>()).Returns(i);
        _captures.GetAllAsync(_userId, Arg.Any<CaptureType?>(), Arg.Any<ProcessingStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<Capture>());
        _captures.GetConfirmedForInitiativeAsync(_userId, i.Id, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Capture>());
        return i;
    }

    [Fact]
    public async Task RefreshAsync_OnSuccess_CreatesPendingProposal()
    {
        var initiative = MakeInitiative();
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult(
                """{"proposedSummary":"new","newDecisions":[{"description":"D1"}],"newRisks":[],"risksToResolve":[],"aiConfidence":0.8,"rationale":"r"}""",
                10, 10, "model", AiProvider.OpenAI));

        await _svc.RefreshAsync(_userId, initiative.Id, CancellationToken.None);

        await _pending.Received(1).AddAsync(
            Arg.Is<PendingBriefUpdate>(p => p.Status == PendingBriefUpdateStatus.Pending && p.Proposal.ProposedSummary == "new"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_OnTasteLimit_CreatesFailedProposal()
    {
        var initiative = MakeInitiative();
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<AiCompletionResult>>(_ => throw new TasteLimitExceededException());

        await _svc.RefreshAsync(_userId, initiative.Id, CancellationToken.None);

        await _pending.Received(1).AddAsync(
            Arg.Is<PendingBriefUpdate>(p => p.Status == PendingBriefUpdateStatus.Failed && p.FailureReason == "Daily AI limit reached"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_OnAiProviderException_CreatesFailedProposal()
    {
        var initiative = MakeInitiative();
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<AiCompletionResult>>(_ => throw new AiProviderException(AiProvider.OpenAI, 500, "boom"));

        await _svc.RefreshAsync(_userId, initiative.Id, CancellationToken.None);

        await _pending.Received(1).AddAsync(
            Arg.Is<PendingBriefUpdate>(p => p.Status == PendingBriefUpdateStatus.Failed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Queue_CoalescesConcurrentTriggersForSameKey()
    {
        var initiativeId = Guid.NewGuid();
        var first = _queue.Enqueue(_userId, initiativeId);
        var second = _queue.Enqueue(_userId, initiativeId);
        var thirdDifferent = _queue.Enqueue(_userId, Guid.NewGuid());

        Assert.True(first);
        Assert.False(second);   // coalesced
        Assert.True(thirdDifferent);
        Assert.Equal(2, _queue.InFlightCount);
    }

    [Fact]
    public async Task RefreshAsync_AutoApplyOn_AppliesProposalAndMarksApplied()
    {
        var initiative = MakeInitiative();
        var user = User.Register("a", "u@e.com", "Bob", null);
        user.UpdatePreferences(UserPreferences.Create(Theme.Light, true, new TimeOnly(8, 0), livingBriefAutoApply: true));
        _users.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);

        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult(
                """{"proposedSummary":"applied","newDecisions":[],"newRisks":[],"risksToResolve":[]}""",
                10, 10, "m", AiProvider.OpenAI));

        await _svc.RefreshAsync(_userId, initiative.Id, CancellationToken.None);

        await _pending.Received(1).AddAsync(
            Arg.Is<PendingBriefUpdate>(p => p.Status == PendingBriefUpdateStatus.Applied),
            Arg.Any<CancellationToken>());
        Assert.Equal("applied", initiative.Brief.Summary);
        Assert.True(initiative.Brief.BriefVersion > 0);
    }

    [Fact]
    public async Task RefreshAsync_AutoApplyOff_LeavesInitiativeUnchanged()
    {
        var initiative = MakeInitiative();
        var user = User.Register("a", "u@e.com", "Bob", null);
        // default auto-apply = false
        _users.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);

        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult(
                """{"proposedSummary":"x","newDecisions":[],"newRisks":[],"risksToResolve":[]}""",
                10, 10, "m", AiProvider.OpenAI));

        await _svc.RefreshAsync(_userId, initiative.Id, CancellationToken.None);

        Assert.Equal(string.Empty, initiative.Brief.Summary);
        Assert.Equal(0, initiative.Brief.BriefVersion);
    }
}

public class ApplyPendingBriefUpdateHandlerTests
{
    private readonly IInitiativeRepository _initiatives = Substitute.For<IInitiativeRepository>();
    private readonly IPendingBriefUpdateRepository _pending = Substitute.For<IPendingBriefUpdateRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task Apply_WhenStale_ThrowsStaleProposalException()
    {
        _currentUser.UserId.Returns(_userId);
        var initiative = Initiative.Create(_userId, "I");
        // bump brief version twice
        initiative.RefreshSummary("a", BriefSource.Manual, []);
        initiative.RefreshSummary("b", BriefSource.Manual, []);

        var proposal = new BriefUpdateProposal { ProposedSummary = "ai" };
        var update = PendingBriefUpdate.Create(_userId, initiative.Id, proposal, briefVersionAtProposal: 0);

        _pending.GetByIdAsync(update.Id, Arg.Any<CancellationToken>()).Returns(update);
        _initiatives.GetByIdAsync(initiative.Id, Arg.Any<CancellationToken>()).Returns(initiative);

        var handler = new ApplyPendingBriefUpdateHandler(_initiatives, _pending, _currentUser, _uow);
        await Assert.ThrowsAsync<ApplyPendingBriefUpdateHandler.StaleProposalException>(() =>
            handler.HandleAsync(update.InitiativeId, update.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Apply_CrossUser_Returns404Equivalent()
    {
        _currentUser.UserId.Returns(_userId);
        var otherUser = Guid.NewGuid();
        var proposal = new BriefUpdateProposal { ProposedSummary = "x" };
        var update = PendingBriefUpdate.Create(otherUser, Guid.NewGuid(), proposal, 0);

        _pending.GetByIdAsync(update.Id, Arg.Any<CancellationToken>()).Returns(update);

        var handler = new ApplyPendingBriefUpdateHandler(_initiatives, _pending, _currentUser, _uow);
        await Assert.ThrowsAsync<MentalMetal.Domain.Common.NotFoundException>(() =>
            handler.HandleAsync(update.InitiativeId, update.Id, CancellationToken.None));
    }
}
