using MentalMetal.Domain.Common;
using MentalMetal.Domain.Interviews;

namespace MentalMetal.Domain.Tests.Interviews;

public class InterviewTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CandidateId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 4, 14, 10, 0, 0, TimeSpan.Zero);

    private static Interview CreateApplied() =>
        Interview.Create(UserId, CandidateId, "Staff Engineer", Now);

    [Fact]
    public void Create_Minimal_InitialStateApplied()
    {
        var interview = CreateApplied();

        Assert.Equal(InterviewStage.Applied, interview.Stage);
        Assert.Equal(UserId, interview.UserId);
        Assert.Equal(CandidateId, interview.CandidatePersonId);
        Assert.Equal("Staff Engineer", interview.RoleTitle);
        Assert.Equal(Now, interview.CreatedAtUtc);
        Assert.Null(interview.CompletedAtUtc);
        Assert.IsType<InterviewCreated>(Assert.Single(interview.DomainEvents));
    }

    [Theory]
    [InlineData(InterviewStage.Applied, InterviewStage.ScreenScheduled)]
    [InlineData(InterviewStage.ScreenScheduled, InterviewStage.ScreenCompleted)]
    [InlineData(InterviewStage.ScreenCompleted, InterviewStage.OnsiteScheduled)]
    [InlineData(InterviewStage.OnsiteScheduled, InterviewStage.OnsiteCompleted)]
    [InlineData(InterviewStage.OnsiteCompleted, InterviewStage.OfferExtended)]
    [InlineData(InterviewStage.OfferExtended, InterviewStage.Hired)]
    public void AdvanceStage_ForwardTransitions_Allowed(InterviewStage from, InterviewStage to)
    {
        var interview = CreateApplied();
        // Walk forward until we reach `from`.
        while (interview.Stage != from)
        {
            var next = interview.Stage switch
            {
                InterviewStage.Applied => InterviewStage.ScreenScheduled,
                InterviewStage.ScreenScheduled => InterviewStage.ScreenCompleted,
                InterviewStage.ScreenCompleted => InterviewStage.OnsiteScheduled,
                InterviewStage.OnsiteScheduled => InterviewStage.OnsiteCompleted,
                InterviewStage.OnsiteCompleted => InterviewStage.OfferExtended,
                _ => throw new InvalidOperationException(),
            };
            interview.AdvanceStage(next, Now);
        }

        interview.ClearDomainEvents();
        interview.AdvanceStage(to, Now.AddHours(1));

        Assert.Equal(to, interview.Stage);
        Assert.IsType<InterviewStageChanged>(Assert.Single(interview.DomainEvents));
    }

    [Fact]
    public void AdvanceStage_InvalidForwardSkip_Throws()
    {
        var interview = CreateApplied();
        var ex = Assert.Throws<DomainException>(() =>
            interview.AdvanceStage(InterviewStage.OnsiteScheduled, Now));
        Assert.Equal(Interview.InvalidStageTransitionCode, ex.Code);
    }

    [Fact]
    public void AdvanceStage_FromTerminal_Throws()
    {
        var interview = CreateApplied();
        interview.AdvanceStage(InterviewStage.Rejected, Now);
        var ex = Assert.Throws<DomainException>(() =>
            interview.AdvanceStage(InterviewStage.ScreenScheduled, Now));
        Assert.Equal(Interview.InvalidStageTransitionCode, ex.Code);
    }

    [Theory]
    [InlineData(InterviewStage.Rejected)]
    [InlineData(InterviewStage.Withdrawn)]
    public void AdvanceStage_AbortFromNonTerminal_Allowed(InterviewStage target)
    {
        var interview = CreateApplied();
        interview.AdvanceStage(InterviewStage.ScreenScheduled, Now);
        interview.AdvanceStage(target, Now.AddHours(1));
        Assert.Equal(target, interview.Stage);
    }

    [Theory]
    [InlineData(InterviewStage.ScreenCompleted)]
    [InlineData(InterviewStage.OnsiteCompleted)]
    [InlineData(InterviewStage.Hired)]
    [InlineData(InterviewStage.Rejected)]
    public void AdvanceStage_CompletionMarkingStages_SetCompletedAtUtc(InterviewStage terminalish)
    {
        var interview = CreateApplied();
        // Walk up to the stage
        var path = terminalish switch
        {
            InterviewStage.ScreenCompleted => new[]
            {
                InterviewStage.ScreenScheduled, InterviewStage.ScreenCompleted,
            },
            InterviewStage.OnsiteCompleted => new[]
            {
                InterviewStage.ScreenScheduled, InterviewStage.ScreenCompleted,
                InterviewStage.OnsiteScheduled, InterviewStage.OnsiteCompleted,
            },
            InterviewStage.Hired => new[]
            {
                InterviewStage.ScreenScheduled, InterviewStage.ScreenCompleted,
                InterviewStage.OnsiteScheduled, InterviewStage.OnsiteCompleted,
                InterviewStage.OfferExtended, InterviewStage.Hired,
            },
            InterviewStage.Rejected => new[] { InterviewStage.Rejected },
            _ => throw new InvalidOperationException(),
        };

        var now = Now;
        foreach (var s in path)
        {
            now = now.AddMinutes(1);
            interview.AdvanceStage(s, now);
        }

        Assert.NotNull(interview.CompletedAtUtc);
    }

    [Fact]
    public void AdvanceStage_OfferExtended_DoesNotSetCompletedAtUtc()
    {
        var interview = CreateApplied();
        var now = Now;
        foreach (var s in new[]
        {
            InterviewStage.ScreenScheduled, InterviewStage.ScreenCompleted,
            InterviewStage.OnsiteScheduled, InterviewStage.OnsiteCompleted,
        })
        {
            now = now.AddMinutes(1);
            interview.AdvanceStage(s, now);
        }

        var preOffer = interview.CompletedAtUtc;
        interview.AdvanceStage(InterviewStage.OfferExtended, now.AddMinutes(1));
        Assert.Equal(preOffer, interview.CompletedAtUtc);
    }

    [Fact]
    public void RecordDecision_BeforeCompletion_Throws()
    {
        var interview = CreateApplied();
        var ex = Assert.Throws<DomainException>(() =>
            interview.RecordDecision(InterviewDecision.Hire, Now));
        Assert.Equal(Interview.DecisionNotAllowedCode, ex.Code);
    }

    [Fact]
    public void RecordDecision_AfterOnsiteCompleted_Allowed()
    {
        var interview = CreateApplied();
        var now = Now;
        foreach (var s in new[]
        {
            InterviewStage.ScreenScheduled, InterviewStage.ScreenCompleted,
            InterviewStage.OnsiteScheduled, InterviewStage.OnsiteCompleted,
        })
        {
            now = now.AddMinutes(1);
            interview.AdvanceStage(s, now);
        }

        interview.RecordDecision(InterviewDecision.Hire, now.AddMinutes(1));
        Assert.Equal(InterviewDecision.Hire, interview.Decision);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void AddScorecard_RatingOutOfRange_Throws(int rating)
    {
        var interview = CreateApplied();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            interview.AddScorecard("System Design", rating, null, Now));
    }

    [Fact]
    public void AddScorecard_EmptyCompetency_Throws()
    {
        var interview = CreateApplied();
        Assert.Throws<ArgumentException>(() =>
            interview.AddScorecard("  ", 3, null, Now));
    }

    [Fact]
    public void SetTranscript_WithBackticks_AcceptedAsIs()
    {
        var interview = CreateApplied();
        interview.SetTranscript("has `code` block", Now);
        Assert.Equal("has `code` block", interview.Transcript!.RawText);
    }

    [Fact]
    public void SetTranscript_ReplacesAndClearsAnalysis()
    {
        var interview = CreateApplied();
        interview.SetTranscript("v1", Now);
        interview.ApplyAnalysis("summary", InterviewDecision.Hire, new[] { "risk" }, "model-a", Now.AddMinutes(1));
        Assert.NotNull(interview.Transcript!.Summary);
        Assert.NotNull(interview.Transcript.AnalyzedAtUtc);

        interview.SetTranscript("v2", Now.AddMinutes(2));

        Assert.Equal("v2", interview.Transcript.RawText);
        Assert.Null(interview.Transcript.Summary);
        Assert.Null(interview.Transcript.RecommendedDecision);
        Assert.Empty(interview.Transcript.RiskSignals);
        Assert.Null(interview.Transcript.AnalyzedAtUtc);
        Assert.Null(interview.Transcript.Model);
    }

    [Fact]
    public void ApplyAnalysis_WithoutTranscript_Throws()
    {
        var interview = CreateApplied();
        var ex = Assert.Throws<DomainException>(() =>
            interview.ApplyAnalysis("s", InterviewDecision.Hire, Array.Empty<string>(), "m", Now));
        Assert.Equal(Interview.TranscriptMissingCode, ex.Code);
    }
}
