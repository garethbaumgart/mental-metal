using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;

namespace MentalMetal.Domain.Tests.Initiatives;

public class LivingBriefTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Empty_ReturnsZeroState()
    {
        var brief = global::MentalMetal.Domain.Initiatives.LivingBrief.LivingBrief.Empty();

        Assert.Equal(string.Empty, brief.Summary);
        Assert.Null(brief.SummaryLastRefreshedAt);
        Assert.Equal(0, brief.BriefVersion);
        Assert.Empty(brief.KeyDecisions);
        Assert.Empty(brief.Risks);
        Assert.Empty(brief.RequirementsHistory);
        Assert.Empty(brief.DesignDirectionHistory);
    }

    [Fact]
    public void Initiative_NewlyCreated_HasEmptyBrief()
    {
        var initiative = Initiative.Create(UserId, "Test");

        Assert.NotNull(initiative.Brief);
        Assert.Equal(0, initiative.Brief.BriefVersion);
        Assert.Empty(initiative.Brief.KeyDecisions);
    }

    [Fact]
    public void RefreshSummary_IncrementsVersionAndRaisesEvent()
    {
        var initiative = Initiative.Create(UserId, "Init");
        initiative.ClearDomainEvents();
        var captureId = Guid.NewGuid();

        initiative.RefreshSummary("New summary", BriefSource.AI, [captureId]);

        Assert.Equal("New summary", initiative.Brief.Summary);
        Assert.Equal(1, initiative.Brief.BriefVersion);
        Assert.NotNull(initiative.Brief.SummaryLastRefreshedAt);
        Assert.Equal(BriefSource.AI, initiative.Brief.SummarySource);
        Assert.Contains(captureId, initiative.Brief.SummarySourceCaptureIds);
        var evt = Assert.Single(initiative.DomainEvents);
        var summaryEvt = Assert.IsType<LivingBriefSummaryUpdated>(evt);
        Assert.Equal(initiative.Id, summaryEvt.InitiativeId);
        Assert.Equal(1, summaryEvt.BriefVersion);
    }

    [Fact]
    public void RecordDecision_AppendsAndRaisesEvent()
    {
        var initiative = Initiative.Create(UserId, "Init");
        initiative.ClearDomainEvents();
        var captureId = Guid.NewGuid();

        var decision = initiative.RecordDecision("Pick Postgres", "Performance", BriefSource.Manual, [captureId]);

        Assert.Equal("Pick Postgres", decision.Description);
        Assert.Equal("Performance", decision.Rationale);
        Assert.Equal(BriefSource.Manual, decision.Source);
        Assert.Contains(captureId, decision.SourceCaptureIds);
        Assert.Single(initiative.Brief.KeyDecisions);
        Assert.Equal(1, initiative.Brief.BriefVersion);
        var evt = Assert.IsType<LivingBriefDecisionLogged>(Assert.Single(initiative.DomainEvents));
        Assert.Equal(decision.Id, evt.DecisionId);
    }

    [Fact]
    public void RaiseRisk_AppendsOpenRiskAndRaisesEvent()
    {
        var initiative = Initiative.Create(UserId, "Init");
        initiative.ClearDomainEvents();

        var risk = initiative.RaiseRisk("Vendor lock-in", RiskSeverity.High, BriefSource.AI, []);

        Assert.Equal(RiskStatus.Open, risk.Status);
        Assert.Equal(RiskSeverity.High, risk.Severity);
        Assert.Null(risk.ResolvedAt);
        Assert.Equal(1, initiative.Brief.BriefVersion);
        Assert.IsType<LivingBriefRiskRaised>(Assert.Single(initiative.DomainEvents));
    }

    [Fact]
    public void ResolveRisk_ForOpenRisk_Succeeds()
    {
        var initiative = Initiative.Create(UserId, "Init");
        var risk = initiative.RaiseRisk("R1", RiskSeverity.Medium, BriefSource.Manual, []);
        initiative.ClearDomainEvents();

        var resolved = initiative.ResolveRisk(risk.Id, "Mitigated");

        Assert.Equal(RiskStatus.Resolved, resolved.Status);
        Assert.NotNull(resolved.ResolvedAt);
        Assert.Equal("Mitigated", resolved.ResolutionNote);
        Assert.Equal(2, initiative.Brief.BriefVersion);
        Assert.IsType<LivingBriefRiskResolved>(Assert.Single(initiative.DomainEvents));
    }

    [Fact]
    public void ResolveRisk_UnknownId_Throws()
    {
        var initiative = Initiative.Create(UserId, "Init");
        Assert.Throws<ArgumentException>(() =>
            initiative.ResolveRisk(Guid.NewGuid(), null));
    }

    [Fact]
    public void ResolveRisk_AlreadyResolved_Throws()
    {
        var initiative = Initiative.Create(UserId, "Init");
        var risk = initiative.RaiseRisk("R1", RiskSeverity.Medium, BriefSource.Manual, []);
        initiative.ResolveRisk(risk.Id, null);

        Assert.Throws<InvalidOperationException>(() =>
            initiative.ResolveRisk(risk.Id, null));
    }

    [Fact]
    public void SnapshotRequirements_Appends_AndSourceIsHonored()
    {
        var initiative = Initiative.Create(UserId, "Init");
        initiative.ClearDomainEvents();
        var captureId = Guid.NewGuid();

        var snap = initiative.SnapshotRequirements("Req v1", BriefSource.Manual, [captureId]);

        Assert.Equal("Req v1", snap.Content);
        Assert.Equal(BriefSource.Manual, snap.Source);
        Assert.Contains(captureId, snap.SourceCaptureIds);
        Assert.Single(initiative.Brief.RequirementsHistory);
    }

    [Fact]
    public void SnapshotDesignDirection_AppendsToHistory()
    {
        var initiative = Initiative.Create(UserId, "Init");
        initiative.SnapshotDesignDirection("D1", BriefSource.AI, []);
        initiative.SnapshotDesignDirection("D2", BriefSource.AI, []);

        Assert.Equal(2, initiative.Brief.DesignDirectionHistory.Count);
    }

    [Fact]
    public void EachBriefMutation_IncrementsVersionExactlyOnce()
    {
        var initiative = Initiative.Create(UserId, "Init");

        initiative.RefreshSummary("s", BriefSource.AI, []);
        Assert.Equal(1, initiative.Brief.BriefVersion);

        initiative.RecordDecision("d", null, BriefSource.AI, []);
        Assert.Equal(2, initiative.Brief.BriefVersion);

        var r = initiative.RaiseRisk("r", RiskSeverity.Low, BriefSource.AI, []);
        Assert.Equal(3, initiative.Brief.BriefVersion);

        initiative.ResolveRisk(r.Id, null);
        Assert.Equal(4, initiative.Brief.BriefVersion);

        initiative.SnapshotRequirements("req", BriefSource.AI, []);
        Assert.Equal(5, initiative.Brief.BriefVersion);

        initiative.SnapshotDesignDirection("dd", BriefSource.AI, []);
        Assert.Equal(6, initiative.Brief.BriefVersion);
    }
}

public class PendingBriefUpdateTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid InitiativeId = Guid.NewGuid();

    private static BriefUpdateProposal SampleProposal() => new()
    {
        ProposedSummary = "s",
        NewDecisions = [new ProposedDecision { Description = "d" }],
        AiConfidence = 0.8m,
        Rationale = "because"
    };

    [Fact]
    public void Create_BeginsPendingAndRaisesProposedEvent()
    {
        var update = PendingBriefUpdate.Create(UserId, InitiativeId, SampleProposal(), briefVersionAtProposal: 3);

        Assert.Equal(PendingBriefUpdateStatus.Pending, update.Status);
        Assert.Equal(3, update.BriefVersionAtProposal);
        Assert.IsType<LivingBriefUpdateProposed>(Assert.Single(update.DomainEvents));
    }

    [Fact]
    public void Edit_FromPending_MovesToEdited()
    {
        var update = PendingBriefUpdate.Create(UserId, InitiativeId, SampleProposal(), 0);
        update.Edit(SampleProposal() with { ProposedSummary = "new" });

        Assert.Equal(PendingBriefUpdateStatus.Edited, update.Status);
        Assert.Equal("new", update.Proposal.ProposedSummary);
    }

    [Fact]
    public void Apply_FromPending_MovesToApplied()
    {
        var update = PendingBriefUpdate.Create(UserId, InitiativeId, SampleProposal(), 0);
        update.MarkApplied();

        Assert.Equal(PendingBriefUpdateStatus.Applied, update.Status);
        Assert.Contains(update.DomainEvents, e => e is LivingBriefUpdateApplied);
    }

    [Fact]
    public void Reject_FromPending_MovesToRejected()
    {
        var update = PendingBriefUpdate.Create(UserId, InitiativeId, SampleProposal(), 0);
        update.Reject("not useful");

        Assert.Equal(PendingBriefUpdateStatus.Rejected, update.Status);
        Assert.Equal("not useful", update.FailureReason);
    }

    [Fact]
    public void Terminal_CannotBeTransitioned()
    {
        var update = PendingBriefUpdate.Create(UserId, InitiativeId, SampleProposal(), 0);
        update.MarkApplied();

        Assert.Throws<InvalidOperationException>(() => update.Reject(null));
        Assert.Throws<InvalidOperationException>(() => update.Edit(SampleProposal()));
        Assert.Throws<InvalidOperationException>(() => update.MarkFailed("x"));
    }

    [Fact]
    public void CreateFailed_ProducesFailedTerminal()
    {
        var update = PendingBriefUpdate.CreateFailed(UserId, InitiativeId, 0, "Daily AI limit reached");

        Assert.Equal(PendingBriefUpdateStatus.Failed, update.Status);
        Assert.Equal("Daily AI limit reached", update.FailureReason);
    }
}
