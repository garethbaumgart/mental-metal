using MentalMetal.Application.Initiatives.Chat;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Delegations;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;
using MentalMetal.Domain.People;
using NSubstitute;

namespace MentalMetal.Application.Tests.Initiatives.Chat;

public class InitiativeChatContextBuilderTests
{
    private readonly IInitiativeRepository _initiatives = Substitute.For<IInitiativeRepository>();
    private readonly ICaptureRepository _captures = Substitute.For<ICaptureRepository>();
    private readonly ICommitmentRepository _commitments = Substitute.For<ICommitmentRepository>();
    private readonly IDelegationRepository _delegations = Substitute.For<IDelegationRepository>();
    private readonly IPersonRepository _people = Substitute.For<IPersonRepository>();
    private readonly InitiativeChatContextBuilder _builder;

    private readonly Guid _userId = Guid.NewGuid();

    public InitiativeChatContextBuilderTests()
    {
        _builder = new InitiativeChatContextBuilder(_initiatives, _captures, _commitments, _delegations, _people);
    }

    [Fact]
    public async Task Build_ReturnsNull_WhenInitiativeBelongsToDifferentUser()
    {
        var foreignUser = Guid.NewGuid();
        var foreignInitiative = Initiative.Create(foreignUser, "Other user's initiative");
        _initiatives.GetByIdAsync(foreignInitiative.Id, Arg.Any<CancellationToken>()).Returns(foreignInitiative);

        var payload = await _builder.BuildAsync(_userId, foreignInitiative.Id, "q", [], CancellationToken.None);

        Assert.Null(payload);
        await _commitments.DidNotReceiveWithAnyArgs().GetAllAsync(default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task Build_EmptyInitiative_ProducesMinimalPayload()
    {
        var initiative = Initiative.Create(_userId, "Empty");
        _initiatives.GetByIdAsync(initiative.Id, Arg.Any<CancellationToken>()).Returns(initiative);
        _commitments.GetAllAsync(_userId, null, null, null, initiative.Id, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());
        _delegations.GetAllAsync(_userId, null, null, null, initiative.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Delegation>());
        _captures.GetConfirmedForInitiativeAsync(_userId, initiative.Id, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Capture>());

        var payload = await _builder.BuildAsync(_userId, initiative.Id, "q", [], CancellationToken.None);

        Assert.NotNull(payload);
        Assert.Equal(initiative.Id, payload.Initiative.Id);
        Assert.Empty(payload.Commitments);
        Assert.Empty(payload.Delegations);
        Assert.Empty(payload.LinkedCaptures);
        Assert.Empty(payload.LivingBrief.RecentDecisions);
        Assert.Empty(payload.LivingBrief.OpenRisks);
        Assert.Null(payload.LivingBrief.LatestRequirementsId);
        Assert.Null(payload.LivingBrief.LatestDesignDirectionId);
    }

    [Fact]
    public async Task Build_CapsApplied_ToCommitmentsAndDecisions()
    {
        var initiative = Initiative.Create(_userId, "Active");
        for (var i = 0; i < InitiativeChatContextBuilder.DecisionCap + 5; i++)
            initiative.RecordDecision($"d{i}", null, BriefSource.Manual, []);

        var personId = Guid.NewGuid();
        var person = Person.Create(_userId, "Alice", PersonType.Stakeholder);
        _people.GetByIdAsync(personId, Arg.Any<CancellationToken>()).Returns(person);

        var many = Enumerable.Range(0, InitiativeChatContextBuilder.CommitmentCap + 20)
            .Select(i => Commitment.Create(_userId, $"c{i}", CommitmentDirection.MineToThem, personId, initiativeId: initiative.Id))
            .ToList();

        _initiatives.GetByIdAsync(initiative.Id, Arg.Any<CancellationToken>()).Returns(initiative);
        _commitments.GetAllAsync(_userId, null, null, null, initiative.Id, null, Arg.Any<CancellationToken>())
            .Returns(many);
        _delegations.GetAllAsync(_userId, null, null, null, initiative.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Delegation>());
        _captures.GetConfirmedForInitiativeAsync(_userId, initiative.Id, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Capture>());

        var payload = await _builder.BuildAsync(_userId, initiative.Id, "q", [], CancellationToken.None);

        Assert.NotNull(payload);
        Assert.Equal(InitiativeChatContextBuilder.CommitmentCap, payload.Commitments.Count);
        Assert.Equal(InitiativeChatContextBuilder.DecisionCap, payload.LivingBrief.RecentDecisions.Count);
    }

    [Fact]
    public async Task KnownCitations_IncludesEveryAssembledEntityId()
    {
        var initiative = Initiative.Create(_userId, "Init");
        var decision = initiative.RecordDecision("Pick Postgres", null, BriefSource.Manual, []);
        var risk = initiative.RaiseRisk("Vendor risk", RiskSeverity.High, BriefSource.Manual, []);
        _initiatives.GetByIdAsync(initiative.Id, Arg.Any<CancellationToken>()).Returns(initiative);
        _commitments.GetAllAsync(_userId, null, null, null, initiative.Id, null, Arg.Any<CancellationToken>())
            .Returns(new List<Commitment>());
        _delegations.GetAllAsync(_userId, null, null, null, initiative.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Delegation>());
        _captures.GetConfirmedForInitiativeAsync(_userId, initiative.Id, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Capture>());

        var payload = await _builder.BuildAsync(_userId, initiative.Id, "q", [], CancellationToken.None);
        Assert.NotNull(payload);
        var known = payload.KnownCitations();

        Assert.Contains((SourceReferenceEntityType.Initiative, initiative.Id), known);
        Assert.Contains((SourceReferenceEntityType.LivingBriefDecision, decision.Id), known);
        Assert.Contains((SourceReferenceEntityType.LivingBriefRisk, risk.Id), known);
    }
}
