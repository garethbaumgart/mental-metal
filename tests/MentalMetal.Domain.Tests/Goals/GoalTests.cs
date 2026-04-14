using MentalMetal.Domain.Goals;

namespace MentalMetal.Domain.Tests.Goals;

public class GoalTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid PersonId = Guid.NewGuid();

    [Fact]
    public void Create_Valid_SetsActiveStatusAndRaisesEvent()
    {
        var goal = Goal.Create(UserId, PersonId, "Get AWS cert", GoalType.Development);

        Assert.Equal(GoalStatus.Active, goal.Status);
        Assert.Equal(GoalType.Development, goal.Type);
        Assert.Equal("Get AWS cert", goal.Title);
        Assert.Null(goal.AchievedAt);

        var evt = Assert.IsType<GoalCreated>(Assert.Single(goal.DomainEvents));
        Assert.Equal(goal.Id, evt.GoalId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_EmptyTitle_Throws(string? title)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Goal.Create(UserId, PersonId, title!, GoalType.Development));
    }

    [Fact]
    public void Create_EmptyPersonId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Goal.Create(UserId, Guid.Empty, "t", GoalType.Development));
    }

    [Fact]
    public void Update_Valid_UpdatesFields()
    {
        var goal = Goal.Create(UserId, PersonId, "old", GoalType.Performance);
        goal.ClearDomainEvents();

        goal.Update("new", "desc", new DateOnly(2026, 12, 31));

        Assert.Equal("new", goal.Title);
        Assert.Equal("desc", goal.Description);
        Assert.Equal(new DateOnly(2026, 12, 31), goal.TargetDate);
        Assert.IsType<GoalUpdated>(Assert.Single(goal.DomainEvents));
    }

    [Fact]
    public void Update_EmptyTitle_Throws()
    {
        var goal = Goal.Create(UserId, PersonId, "old", GoalType.Performance);
        Assert.ThrowsAny<ArgumentException>(() => goal.Update("", null, null));
    }

    [Fact]
    public void Achieve_FromActive_Transitions()
    {
        var goal = Goal.Create(UserId, PersonId, "t", GoalType.Performance);
        goal.ClearDomainEvents();

        goal.Achieve();

        Assert.Equal(GoalStatus.Achieved, goal.Status);
        Assert.NotNull(goal.AchievedAt);
        Assert.IsType<GoalAchieved>(Assert.Single(goal.DomainEvents));
    }

    [Fact]
    public void Achieve_FromAchieved_Throws()
    {
        var goal = Goal.Create(UserId, PersonId, "t", GoalType.Performance);
        goal.Achieve();

        Assert.Throws<InvalidOperationException>(() => goal.Achieve());
    }

    [Fact]
    public void Miss_FromActive_Transitions()
    {
        var goal = Goal.Create(UserId, PersonId, "t", GoalType.Performance);
        goal.Miss();

        Assert.Equal(GoalStatus.Missed, goal.Status);
    }

    [Fact]
    public void Defer_FromActive_StoresReason()
    {
        var goal = Goal.Create(UserId, PersonId, "t", GoalType.Performance);
        goal.ClearDomainEvents();

        goal.Defer("Reprioritized");

        Assert.Equal(GoalStatus.Deferred, goal.Status);
        Assert.Equal("Reprioritized", goal.DeferralReason);
        Assert.IsType<GoalDeferred>(Assert.Single(goal.DomainEvents));
    }

    [Fact]
    public void Reactivate_FromAchieved_ClearsAchievedAt()
    {
        var goal = Goal.Create(UserId, PersonId, "t", GoalType.Performance);
        goal.Achieve();
        goal.ClearDomainEvents();

        goal.Reactivate();

        Assert.Equal(GoalStatus.Active, goal.Status);
        Assert.Null(goal.AchievedAt);
        Assert.IsType<GoalReactivated>(Assert.Single(goal.DomainEvents));
    }

    [Fact]
    public void Reactivate_FromActive_Throws()
    {
        var goal = Goal.Create(UserId, PersonId, "t", GoalType.Performance);
        Assert.Throws<InvalidOperationException>(() => goal.Reactivate());
    }

    [Fact]
    public void Reactivate_FromDeferred_ClearsReason()
    {
        var goal = Goal.Create(UserId, PersonId, "t", GoalType.Performance);
        goal.Defer("reason");
        goal.Reactivate();

        Assert.Null(goal.DeferralReason);
    }

    [Fact]
    public void RecordCheckIn_Valid_AppendsCheckIn()
    {
        var goal = Goal.Create(UserId, PersonId, "t", GoalType.Performance);
        goal.ClearDomainEvents();

        var c = goal.RecordCheckIn("3 of 5 done", 60);

        Assert.Single(goal.CheckIns);
        Assert.Equal(60, c.Progress);
        Assert.IsType<GoalCheckInRecorded>(Assert.Single(goal.DomainEvents));
    }

    [Fact]
    public void RecordCheckIn_EmptyNote_Throws()
    {
        var goal = Goal.Create(UserId, PersonId, "t", GoalType.Performance);
        Assert.ThrowsAny<ArgumentException>(() => goal.RecordCheckIn("", 50));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void RecordCheckIn_InvalidProgress_Throws(int progress)
    {
        var goal = Goal.Create(UserId, PersonId, "t", GoalType.Performance);
        Assert.Throws<ArgumentException>(() => goal.RecordCheckIn("note", progress));
    }

    [Fact]
    public void RecordCheckIn_NullProgress_Allowed()
    {
        var goal = Goal.Create(UserId, PersonId, "t", GoalType.Performance);
        goal.RecordCheckIn("discussed blockers", null);
        Assert.Null(goal.CheckIns[0].Progress);
    }
}
