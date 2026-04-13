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
        Assert.Empty(initiative.Milestones);
        Assert.Empty(initiative.LinkedPersonIds);
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
        var domainEvent = Assert.Single(initiative.DomainEvents);
        Assert.IsType<InitiativeStatusChanged>(domainEvent);
    }

    [Fact]
    public void ChangeStatus_ActiveToCompleted_Succeeds()
    {
        var initiative = Initiative.Create(UserId, "Test");
        initiative.ClearDomainEvents();

        initiative.ChangeStatus(InitiativeStatus.Completed);

        Assert.Equal(InitiativeStatus.Completed, initiative.Status);
    }

    [Fact]
    public void ChangeStatus_ActiveToCancelled_Succeeds()
    {
        var initiative = Initiative.Create(UserId, "Test");
        initiative.ClearDomainEvents();

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
    public void AddMilestone_OnActive_SucceedsAndRaisesEvent()
    {
        var initiative = Initiative.Create(UserId, "Test");
        initiative.ClearDomainEvents();

        initiative.AddMilestone("Phase 1", new DateOnly(2026, 6, 1), "First phase");

        Assert.Single(initiative.Milestones);
        Assert.Equal("Phase 1", initiative.Milestones[0].Title);
        Assert.Equal(new DateOnly(2026, 6, 1), initiative.Milestones[0].TargetDate);
        Assert.Equal("First phase", initiative.Milestones[0].Description);
        Assert.False(initiative.Milestones[0].IsCompleted);

        var domainEvent = Assert.Single(initiative.DomainEvents);
        var set = Assert.IsType<MilestoneSet>(domainEvent);
        Assert.Equal(initiative.Milestones[0].Id, set.MilestoneId);
    }

    [Fact]
    public void UpdateMilestone_OnActive_SucceedsAndRaisesEvent()
    {
        var initiative = Initiative.Create(UserId, "Test");
        initiative.AddMilestone("Phase 1", new DateOnly(2026, 6, 1));
        var milestoneId = initiative.Milestones[0].Id;
        initiative.ClearDomainEvents();

        initiative.UpdateMilestone(milestoneId, "Phase 1 Updated", new DateOnly(2026, 7, 1), "Updated desc");

        Assert.Single(initiative.Milestones);
        Assert.Equal(milestoneId, initiative.Milestones[0].Id);
        Assert.Equal("Phase 1 Updated", initiative.Milestones[0].Title);
        Assert.Equal(new DateOnly(2026, 7, 1), initiative.Milestones[0].TargetDate);
        Assert.Equal("Updated desc", initiative.Milestones[0].Description);

        var domainEvent = Assert.Single(initiative.DomainEvents);
        Assert.IsType<MilestoneSet>(domainEvent);
    }

    [Fact]
    public void UpdateMilestone_NotFound_Throws()
    {
        var initiative = Initiative.Create(UserId, "Test");

        Assert.ThrowsAny<ArgumentException>(() =>
            initiative.UpdateMilestone(Guid.NewGuid(), "Title", new DateOnly(2026, 6, 1)));
    }

    [Fact]
    public void RemoveMilestone_OnActive_SucceedsAndRaisesEvent()
    {
        var initiative = Initiative.Create(UserId, "Test");
        initiative.AddMilestone("Phase 1", new DateOnly(2026, 6, 1));
        var milestoneId = initiative.Milestones[0].Id;
        initiative.ClearDomainEvents();

        initiative.RemoveMilestone(milestoneId);

        Assert.Empty(initiative.Milestones);
        var domainEvent = Assert.Single(initiative.DomainEvents);
        Assert.IsType<MilestoneRemoved>(domainEvent);
    }

    [Fact]
    public void RemoveMilestone_NotFound_Throws()
    {
        var initiative = Initiative.Create(UserId, "Test");

        Assert.ThrowsAny<ArgumentException>(() =>
            initiative.RemoveMilestone(Guid.NewGuid()));
    }

    [Fact]
    public void CompleteMilestone_OnActive_SucceedsAndRaisesEvent()
    {
        var initiative = Initiative.Create(UserId, "Test");
        initiative.AddMilestone("Phase 1", new DateOnly(2026, 6, 1));
        var milestoneId = initiative.Milestones[0].Id;
        initiative.ClearDomainEvents();

        initiative.CompleteMilestone(milestoneId);

        Assert.True(initiative.Milestones[0].IsCompleted);
        var domainEvent = Assert.Single(initiative.DomainEvents);
        var completed = Assert.IsType<MilestoneCompleted>(domainEvent);
        Assert.Equal(milestoneId, completed.MilestoneId);
    }

    [Fact]
    public void CompleteMilestone_NotFound_Throws()
    {
        var initiative = Initiative.Create(UserId, "Test");

        Assert.ThrowsAny<ArgumentException>(() =>
            initiative.CompleteMilestone(Guid.NewGuid()));
    }

    [Theory]
    [InlineData(InitiativeStatus.Completed)]
    [InlineData(InitiativeStatus.Cancelled)]
    public void AddMilestone_OnTerminalStatus_Throws(InitiativeStatus terminalStatus)
    {
        var initiative = CreateWithStatus(terminalStatus);

        Assert.ThrowsAny<ArgumentException>(() =>
            initiative.AddMilestone("Phase 1", new DateOnly(2026, 6, 1)));
    }

    [Theory]
    [InlineData(InitiativeStatus.Completed)]
    [InlineData(InitiativeStatus.Cancelled)]
    public void UpdateMilestone_OnTerminalStatus_Throws(InitiativeStatus terminalStatus)
    {
        var initiative = Initiative.Create(UserId, "Test");
        initiative.AddMilestone("Phase 1", new DateOnly(2026, 6, 1));
        var milestoneId = initiative.Milestones[0].Id;
        initiative.ChangeStatus(terminalStatus);

        Assert.ThrowsAny<ArgumentException>(() =>
            initiative.UpdateMilestone(milestoneId, "Updated", new DateOnly(2026, 7, 1)));
    }

    [Theory]
    [InlineData(InitiativeStatus.Completed)]
    [InlineData(InitiativeStatus.Cancelled)]
    public void RemoveMilestone_OnTerminalStatus_Throws(InitiativeStatus terminalStatus)
    {
        var initiative = Initiative.Create(UserId, "Test");
        initiative.AddMilestone("Phase 1", new DateOnly(2026, 6, 1));
        var milestoneId = initiative.Milestones[0].Id;
        initiative.ChangeStatus(terminalStatus);

        Assert.ThrowsAny<ArgumentException>(() =>
            initiative.RemoveMilestone(milestoneId));
    }

    [Theory]
    [InlineData(InitiativeStatus.Completed)]
    [InlineData(InitiativeStatus.Cancelled)]
    public void CompleteMilestone_OnTerminalStatus_Throws(InitiativeStatus terminalStatus)
    {
        var initiative = Initiative.Create(UserId, "Test");
        initiative.AddMilestone("Phase 1", new DateOnly(2026, 6, 1));
        var milestoneId = initiative.Milestones[0].Id;
        initiative.ChangeStatus(terminalStatus);

        Assert.ThrowsAny<ArgumentException>(() =>
            initiative.CompleteMilestone(milestoneId));
    }

    [Fact]
    public void LinkPerson_OnActive_AddsAndRaisesEvent()
    {
        var initiative = Initiative.Create(UserId, "Test");
        initiative.ClearDomainEvents();
        var personId = Guid.NewGuid();

        initiative.LinkPerson(personId);

        Assert.Single(initiative.LinkedPersonIds);
        Assert.Contains(personId, initiative.LinkedPersonIds);
        var domainEvent = Assert.Single(initiative.DomainEvents);
        var linked = Assert.IsType<PersonLinkedToInitiative>(domainEvent);
        Assert.Equal(personId, linked.PersonId);
    }

    [Fact]
    public void LinkPerson_Duplicate_IsIdempotent()
    {
        var initiative = Initiative.Create(UserId, "Test");
        var personId = Guid.NewGuid();
        initiative.LinkPerson(personId);
        initiative.ClearDomainEvents();

        initiative.LinkPerson(personId);

        Assert.Single(initiative.LinkedPersonIds);
        Assert.Empty(initiative.DomainEvents);
    }

    [Fact]
    public void UnlinkPerson_OnActive_RemovesAndRaisesEvent()
    {
        var initiative = Initiative.Create(UserId, "Test");
        var personId = Guid.NewGuid();
        initiative.LinkPerson(personId);
        initiative.ClearDomainEvents();

        initiative.UnlinkPerson(personId);

        Assert.Empty(initiative.LinkedPersonIds);
        var domainEvent = Assert.Single(initiative.DomainEvents);
        var unlinked = Assert.IsType<PersonUnlinkedFromInitiative>(domainEvent);
        Assert.Equal(personId, unlinked.PersonId);
    }

    [Fact]
    public void UnlinkPerson_NotLinked_Throws()
    {
        var initiative = Initiative.Create(UserId, "Test");

        Assert.ThrowsAny<ArgumentException>(() =>
            initiative.UnlinkPerson(Guid.NewGuid()));
    }

    [Theory]
    [InlineData(InitiativeStatus.Completed)]
    [InlineData(InitiativeStatus.Cancelled)]
    public void LinkPerson_OnTerminalStatus_Throws(InitiativeStatus terminalStatus)
    {
        var initiative = CreateWithStatus(terminalStatus);

        Assert.ThrowsAny<ArgumentException>(() =>
            initiative.LinkPerson(Guid.NewGuid()));
    }

    [Theory]
    [InlineData(InitiativeStatus.Completed)]
    [InlineData(InitiativeStatus.Cancelled)]
    public void UnlinkPerson_OnTerminalStatus_Throws(InitiativeStatus terminalStatus)
    {
        var initiative = Initiative.Create(UserId, "Test");
        var personId = Guid.NewGuid();
        initiative.LinkPerson(personId);
        initiative.ChangeStatus(terminalStatus);

        Assert.ThrowsAny<ArgumentException>(() =>
            initiative.UnlinkPerson(personId));
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
