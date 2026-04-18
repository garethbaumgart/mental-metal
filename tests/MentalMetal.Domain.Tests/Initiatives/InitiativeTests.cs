using MentalMetal.Domain.Initiatives;

namespace MentalMetal.Domain.Tests.Initiatives;

public class InitiativeTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_ValidTitle_CreatesInitiativeWithCorrectState()
    {
        var initiative = Initiative.Create(UserId, "Q1 Hiring Push");

        Assert.NotEqual(Guid.Empty, initiative.Id);
        Assert.Equal(UserId, initiative.UserId);
        Assert.Equal("Q1 Hiring Push", initiative.Title);
        Assert.Equal(InitiativeStatus.Active, initiative.Status);
        Assert.Null(initiative.AutoSummary);
        Assert.Null(initiative.LastSummaryRefreshedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyTitle_Throws(string? title)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Initiative.Create(UserId, title!));
    }

    [Fact]
    public void Create_SetsStatusToActive()
    {
        var initiative = Initiative.Create(UserId, "Test");

        Assert.Equal(InitiativeStatus.Active, initiative.Status);
    }

    [Fact]
    public void Create_RaisesInitiativeCreatedEvent()
    {
        var initiative = Initiative.Create(UserId, "Q1 Hiring Push");

        var domainEvent = Assert.Single(initiative.DomainEvents);
        var created = Assert.IsType<InitiativeCreated>(domainEvent);
        Assert.Equal(initiative.Id, created.InitiativeId);
        Assert.Equal(UserId, created.UserId);
        Assert.Equal("Q1 Hiring Push", created.Title);
    }

    [Fact]
    public void UpdateTitle_OnActive_SucceedsAndRaisesEvent()
    {
        var initiative = Initiative.Create(UserId, "Old Title");
        initiative.ClearDomainEvents();

        initiative.UpdateTitle("New Title");

        Assert.Equal("New Title", initiative.Title);
        var domainEvent = Assert.Single(initiative.DomainEvents);
        Assert.IsType<InitiativeTitleUpdated>(domainEvent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void UpdateTitle_EmptyTitle_Throws(string? title)
    {
        var initiative = Initiative.Create(UserId, "Test");

        Assert.ThrowsAny<ArgumentException>(() =>
            initiative.UpdateTitle(title!));
    }

    [Theory]
    [InlineData(InitiativeStatus.Completed)]
    [InlineData(InitiativeStatus.Cancelled)]
    public void UpdateTitle_OnTerminalStatus_Throws(InitiativeStatus terminalStatus)
    {
        var initiative = CreateWithStatus(terminalStatus);

        Assert.ThrowsAny<ArgumentException>(() =>
            initiative.UpdateTitle("New Title"));
    }

    [Fact]
    public void ChangeStatus_ActiveToOnHold_Succeeds()
    {
        var initiative = Initiative.Create(UserId, "Test");
        initiative.ClearDomainEvents();

        initiative.ChangeStatus(InitiativeStatus.OnHold);

        Assert.Equal(InitiativeStatus.OnHold, initiative.Status);
        var domainEvent = Assert.Single(initiative.DomainEvents);
        var changed = Assert.IsType<InitiativeStatusChanged>(domainEvent);
        Assert.Equal(InitiativeStatus.Active, changed.OldStatus);
        Assert.Equal(InitiativeStatus.OnHold, changed.NewStatus);
    }

    [Fact]
    public void ChangeStatus_OnHoldToActive_Succeeds()
    {
        var initiative = Initiative.Create(UserId, "Test");
        initiative.ChangeStatus(InitiativeStatus.OnHold);
        initiative.ClearDomainEvents();

        initiative.ChangeStatus(InitiativeStatus.Active);

        Assert.Equal(InitiativeStatus.Active, initiative.Status);
    }

    [Fact]
    public void ChangeStatus_ActiveToCompleted_Succeeds()
    {
        var initiative = Initiative.Create(UserId, "Test");
        initiative.ChangeStatus(InitiativeStatus.Completed);
        Assert.Equal(InitiativeStatus.Completed, initiative.Status);
    }

    [Fact]
    public void ChangeStatus_ActiveToCancelled_Succeeds()
    {
        var initiative = Initiative.Create(UserId, "Test");
        initiative.ChangeStatus(InitiativeStatus.Cancelled);
        Assert.Equal(InitiativeStatus.Cancelled, initiative.Status);
    }

    [Fact]
    public void ChangeStatus_OnHoldToCompleted_Throws()
    {
        var initiative = Initiative.Create(UserId, "Test");
        initiative.ChangeStatus(InitiativeStatus.OnHold);

        Assert.ThrowsAny<ArgumentException>(() =>
            initiative.ChangeStatus(InitiativeStatus.Completed));
    }

    [Theory]
    [InlineData(InitiativeStatus.Completed)]
    [InlineData(InitiativeStatus.Cancelled)]
    public void ChangeStatus_FromTerminal_Throws(InitiativeStatus terminalStatus)
    {
        var initiative = CreateWithStatus(terminalStatus);

        Assert.ThrowsAny<ArgumentException>(() =>
            initiative.ChangeStatus(InitiativeStatus.Active));
    }

    [Fact]
    public void ChangeStatus_SameStatus_IsNoOp()
    {
        var initiative = Initiative.Create(UserId, "Test");
        initiative.ClearDomainEvents();

        initiative.ChangeStatus(InitiativeStatus.Active);

        Assert.Empty(initiative.DomainEvents);
    }

    [Fact]
    public void RefreshAutoSummary_SetsSummaryAndTimestamp()
    {
        var initiative = Initiative.Create(UserId, "Test");
        initiative.ClearDomainEvents();

        initiative.RefreshAutoSummary("This is the AI-generated summary.");

        Assert.Equal("This is the AI-generated summary.", initiative.AutoSummary);
        Assert.NotNull(initiative.LastSummaryRefreshedAt);

        var domainEvent = Assert.Single(initiative.DomainEvents);
        Assert.IsType<InitiativeSummaryRefreshed>(domainEvent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void RefreshAutoSummary_EmptySummary_Throws(string? summary)
    {
        var initiative = Initiative.Create(UserId, "Test");

        Assert.ThrowsAny<ArgumentException>(() =>
            initiative.RefreshAutoSummary(summary!));
    }

    [Fact]
    public void Create_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Initiative.Create(Guid.Empty, "Test"));
    }

    private static Initiative CreateWithStatus(InitiativeStatus status)
    {
        var initiative = Initiative.Create(UserId, "Test");
        if (status != InitiativeStatus.Active)
            initiative.ChangeStatus(status);
        return initiative;
    }
}
