using MentalMetal.Domain.Common;
using MentalMetal.Domain.Nudges;

namespace MentalMetal.Domain.Tests.Nudges;

public class NudgeTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 4, 14); // Tuesday

    [Fact]
    public void Create_Daily_SetsNextDueDateToToday()
    {
        var nudge = Nudge.Create(UserId, "Review risk log", NudgeCadence.Daily(), Today);

        Assert.Equal(UserId, nudge.UserId);
        Assert.Equal("Review risk log", nudge.Title);
        Assert.Equal(Today, nudge.NextDueDate);
        Assert.True(nudge.IsActive);
        Assert.Null(nudge.LastNudgedAt);
        Assert.Contains(nudge.DomainEvents, e => e is NudgeCreated);
    }

    [Fact]
    public void Create_EmptyTitle_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Nudge.Create(UserId, "", NudgeCadence.Daily(), Today));
    }

    [Fact]
    public void Create_TitleTooLong_Throws()
    {
        var longTitle = new string('a', Nudge.MaxTitleLength + 1);
        Assert.Throws<ArgumentException>(() =>
            Nudge.Create(UserId, longTitle, NudgeCadence.Daily(), Today));
    }

    [Fact]
    public void Create_NotesTooLong_Throws()
    {
        var longNotes = new string('a', Nudge.MaxNotesLength + 1);
        Assert.Throws<ArgumentException>(() =>
            Nudge.Create(UserId, "ok", NudgeCadence.Daily(), Today, notes: longNotes));
    }

    [Fact]
    public void Create_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Nudge.Create(Guid.Empty, "ok", NudgeCadence.Daily(), Today));
    }

    [Fact]
    public void MarkNudged_Daily_AdvancesToTomorrow()
    {
        var nudge = Nudge.Create(UserId, "t", NudgeCadence.Daily(), Today);
        nudge.MarkNudged(Today);
        Assert.Equal(Today.AddDays(1), nudge.NextDueDate);
        Assert.NotNull(nudge.LastNudgedAt);
        Assert.Contains(nudge.DomainEvents, e => e is NudgeNudged);
    }

    [Fact]
    public void MarkNudged_Paused_Throws()
    {
        var nudge = Nudge.Create(UserId, "t", NudgeCadence.Daily(), Today);
        nudge.Pause();
        var ex = Assert.Throws<DomainException>(() => nudge.MarkNudged(Today));
        Assert.Equal("nudge.notActive", ex.Code);
    }

    [Fact]
    public void Pause_Active_SetsInactive()
    {
        var nudge = Nudge.Create(UserId, "t", NudgeCadence.Daily(), Today);
        nudge.Pause();
        Assert.False(nudge.IsActive);
        Assert.Contains(nudge.DomainEvents, e => e is NudgePaused);
    }

    [Fact]
    public void Pause_AlreadyPaused_Throws()
    {
        var nudge = Nudge.Create(UserId, "t", NudgeCadence.Daily(), Today);
        nudge.Pause();
        var ex = Assert.Throws<DomainException>(() => nudge.Pause());
        Assert.Equal("nudge.alreadyPaused", ex.Code);
    }

    [Fact]
    public void Resume_Paused_ReactivatesAndRecomputesNextDueDate()
    {
        var cadence = NudgeCadence.Weekly(DayOfWeek.Thursday);
        var nudge = Nudge.Create(UserId, "t", cadence, new DateOnly(2026, 4, 1));
        nudge.Pause();

        var resumeDay = new DateOnly(2026, 4, 20); // a Monday
        nudge.Resume(resumeDay);

        Assert.True(nudge.IsActive);
        Assert.Equal(new DateOnly(2026, 4, 23), nudge.NextDueDate); // next Thursday
        Assert.Contains(nudge.DomainEvents, e => e is NudgeResumed);
    }

    [Fact]
    public void Resume_Active_Throws()
    {
        var nudge = Nudge.Create(UserId, "t", NudgeCadence.Daily(), Today);
        var ex = Assert.Throws<DomainException>(() => nudge.Resume(Today));
        Assert.Equal("nudge.alreadyActive", ex.Code);
    }

    [Fact]
    public void UpdateCadence_RecomputesNextDueDate()
    {
        var nudge = Nudge.Create(UserId, "t", NudgeCadence.Daily(), Today);
        nudge.UpdateCadence(NudgeCadence.Weekly(DayOfWeek.Friday), new DateOnly(2026, 4, 14));

        Assert.Equal(new DateOnly(2026, 4, 17), nudge.NextDueDate);
        Assert.Contains(nudge.DomainEvents, e => e is NudgeCadenceChanged);
    }

    [Fact]
    public void UpdateDetails_ChangesFieldsAndRaisesEvent()
    {
        var nudge = Nudge.Create(UserId, "original", NudgeCadence.Daily(), Today);
        nudge.ClearDomainEvents();

        nudge.UpdateDetails("updated", "notes", Guid.NewGuid(), null);

        Assert.Equal("updated", nudge.Title);
        Assert.Equal("notes", nudge.Notes);
        Assert.Contains(nudge.DomainEvents, e => e is NudgeUpdated);
    }

    [Fact]
    public void UpdateDetails_NoChange_DoesNotRaiseEvent()
    {
        var nudge = Nudge.Create(UserId, "original", NudgeCadence.Daily(), Today);
        nudge.ClearDomainEvents();

        nudge.UpdateDetails("original", null, null, null);

        Assert.DoesNotContain(nudge.DomainEvents, e => e is NudgeUpdated);
    }

    [Fact]
    public void Create_WithStartDateInFuture_NextDueDateFromStartDate()
    {
        var start = new DateOnly(2026, 5, 1);
        var nudge = Nudge.Create(UserId, "t", NudgeCadence.Weekly(DayOfWeek.Thursday), Today, startDate: start);
        // May 1 2026 is a Friday, so first Thursday on-or-after is May 7.
        Assert.Equal(new DateOnly(2026, 5, 7), nudge.NextDueDate);
    }

    [Fact]
    public void MarkNudged_MonthlyDay31_JanuaryClampsToFeb28()
    {
        var nudge = Nudge.Create(UserId, "t", NudgeCadence.Monthly(31), new DateOnly(2026, 1, 31));
        Assert.Equal(new DateOnly(2026, 1, 31), nudge.NextDueDate);
        nudge.MarkNudged(new DateOnly(2026, 1, 31));
        Assert.Equal(new DateOnly(2026, 2, 28), nudge.NextDueDate);
    }
}
