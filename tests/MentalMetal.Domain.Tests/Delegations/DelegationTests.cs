using MentalMetal.Domain.Delegations;

namespace MentalMetal.Domain.Tests.Delegations;

public class DelegationTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid PersonId = Guid.NewGuid();

    // 2.1 Test Delegation creation with valid inputs and domain event
    [Fact]
    public void Create_ValidInputs_CreatesDelegationWithCorrectState()
    {
        var delegation = Delegation.Create(UserId, "Write the API spec for payments service", PersonId);

        Assert.NotEqual(Guid.Empty, delegation.Id);
        Assert.Equal(UserId, delegation.UserId);
        Assert.Equal("Write the API spec for payments service", delegation.Description);
        Assert.Equal(PersonId, delegation.DelegatePersonId);
        Assert.Equal(DelegationStatus.Assigned, delegation.Status);
        Assert.Equal(Priority.Medium, delegation.Priority);
        Assert.Null(delegation.InitiativeId);
        Assert.Null(delegation.SourceCaptureId);
        Assert.Null(delegation.DueDate);
        Assert.Null(delegation.CompletedAt);
        Assert.Null(delegation.Notes);
        Assert.Null(delegation.LastFollowedUpAt);

        var domainEvent = Assert.Single(delegation.DomainEvents);
        var created = Assert.IsType<DelegationCreated>(domainEvent);
        Assert.Equal(delegation.Id, created.DelegationId);
        Assert.Equal(UserId, created.UserId);
        Assert.Equal(PersonId, created.DelegatePersonId);
    }

    [Fact]
    public void Create_WithAllOptionalFields_SetsAllFields()
    {
        var initiativeId = Guid.NewGuid();
        var captureId = Guid.NewGuid();
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        var delegation = Delegation.Create(
            UserId, "Deliver design doc", PersonId,
            dueDate, initiativeId, Priority.High, captureId);

        Assert.Equal(dueDate, delegation.DueDate);
        Assert.Equal(initiativeId, delegation.InitiativeId);
        Assert.Equal(Priority.High, delegation.Priority);
        Assert.Equal(captureId, delegation.SourceCaptureId);
    }

    // 2.2 Test creation rejects empty description and missing delegatePersonId
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyDescription_Throws(string? description)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Delegation.Create(UserId, description!, PersonId));
    }

    [Fact]
    public void Create_EmptyDelegatePersonId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Delegation.Create(UserId, "Test", Guid.Empty));
    }

    // 2.3 Test valid status transitions
    [Fact]
    public void MarkInProgress_FromAssigned_TransitionsToInProgress()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        delegation.ClearDomainEvents();

        delegation.MarkInProgress();

        Assert.Equal(DelegationStatus.InProgress, delegation.Status);
        var domainEvent = Assert.Single(delegation.DomainEvents);
        Assert.IsType<DelegationStarted>(domainEvent);
    }

    [Fact]
    public void MarkCompleted_FromAssigned_TransitionsToCompleted()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        delegation.ClearDomainEvents();

        delegation.MarkCompleted();

        Assert.Equal(DelegationStatus.Completed, delegation.Status);
        var domainEvent = Assert.Single(delegation.DomainEvents);
        Assert.IsType<DelegationCompleted>(domainEvent);
    }

    [Fact]
    public void MarkBlocked_FromAssigned_TransitionsToBlocked()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        delegation.ClearDomainEvents();

        delegation.MarkBlocked("Waiting on third-party API access");

        Assert.Equal(DelegationStatus.Blocked, delegation.Status);
        var domainEvent = Assert.Single(delegation.DomainEvents);
        var blocked = Assert.IsType<DelegationBlocked>(domainEvent);
        Assert.Equal("Waiting on third-party API access", blocked.Reason);
    }

    [Fact]
    public void MarkCompleted_FromInProgress_TransitionsToCompleted()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        delegation.MarkInProgress();
        delegation.ClearDomainEvents();

        delegation.MarkCompleted();

        Assert.Equal(DelegationStatus.Completed, delegation.Status);
        var domainEvent = Assert.Single(delegation.DomainEvents);
        Assert.IsType<DelegationCompleted>(domainEvent);
    }

    [Fact]
    public void MarkBlocked_FromInProgress_TransitionsToBlocked()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        delegation.MarkInProgress();
        delegation.ClearDomainEvents();

        delegation.MarkBlocked("Dependency not ready");

        Assert.Equal(DelegationStatus.Blocked, delegation.Status);
    }

    [Fact]
    public void Unblock_FromBlocked_TransitionsToInProgress()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        delegation.MarkInProgress();
        delegation.MarkBlocked("Waiting");
        delegation.ClearDomainEvents();

        delegation.Unblock();

        Assert.Equal(DelegationStatus.InProgress, delegation.Status);
        var domainEvent = Assert.Single(delegation.DomainEvents);
        Assert.IsType<DelegationUnblocked>(domainEvent);
    }

    [Fact]
    public void MarkCompleted_FromBlocked_TransitionsToCompleted()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        delegation.MarkInProgress();
        delegation.MarkBlocked("Waiting");
        delegation.ClearDomainEvents();

        delegation.MarkCompleted("Done despite blocker");

        Assert.Equal(DelegationStatus.Completed, delegation.Status);
    }

    // 2.4 Test invalid status transitions throw domain exception
    [Fact]
    public void MarkCompleted_FromCompleted_Throws()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        delegation.MarkCompleted();

        Assert.Throws<InvalidOperationException>(() => delegation.MarkCompleted());
    }

    [Fact]
    public void MarkInProgress_FromInProgress_Throws()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        delegation.MarkInProgress();

        Assert.Throws<InvalidOperationException>(() => delegation.MarkInProgress());
    }

    [Fact]
    public void MarkInProgress_FromCompleted_Throws()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        delegation.MarkCompleted();

        Assert.Throws<InvalidOperationException>(() => delegation.MarkInProgress());
    }

    [Fact]
    public void MarkBlocked_FromCompleted_Throws()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        delegation.MarkCompleted();

        Assert.Throws<InvalidOperationException>(() => delegation.MarkBlocked("reason"));
    }

    [Fact]
    public void MarkBlocked_FromBlocked_Throws()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        delegation.MarkBlocked("first");

        Assert.Throws<InvalidOperationException>(() => delegation.MarkBlocked("second"));
    }

    [Fact]
    public void Unblock_FromAssigned_Throws()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);

        Assert.Throws<InvalidOperationException>(() => delegation.Unblock());
    }

    [Fact]
    public void Unblock_FromCompleted_Throws()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        delegation.MarkCompleted();

        Assert.Throws<InvalidOperationException>(() => delegation.Unblock());
    }

    // 2.5 Test CompletedAt is set on completion
    [Fact]
    public void MarkCompleted_SetsCompletedAt()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);

        delegation.MarkCompleted();

        Assert.NotNull(delegation.CompletedAt);
    }

    // 2.6 Test RecordFollowUp updates LastFollowedUpAt
    [Fact]
    public void RecordFollowUp_UpdatesLastFollowedUpAt()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        Assert.Null(delegation.LastFollowedUpAt);

        delegation.RecordFollowUp("Checked in, on track");

        Assert.NotNull(delegation.LastFollowedUpAt);
        var domainEvent = delegation.DomainEvents.OfType<DelegationFollowedUp>().Single();
        Assert.Equal("Checked in, on track", domainEvent.Notes);
    }

    [Fact]
    public void RecordFollowUp_WithoutNotes_StillUpdatesTimestamp()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);

        delegation.RecordFollowUp();

        Assert.NotNull(delegation.LastFollowedUpAt);
    }

    // 2.7 Test Reassign changes DelegatePersonId and raises event (idempotent for same person)
    [Fact]
    public void Reassign_ChangesPersonIdAndRaisesEvent()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        delegation.ClearDomainEvents();
        var newPersonId = Guid.NewGuid();

        delegation.Reassign(newPersonId);

        Assert.Equal(newPersonId, delegation.DelegatePersonId);
        var domainEvent = Assert.Single(delegation.DomainEvents);
        var reassigned = Assert.IsType<DelegationReassigned>(domainEvent);
        Assert.Equal(PersonId, reassigned.OldPersonId);
        Assert.Equal(newPersonId, reassigned.NewPersonId);
    }

    [Fact]
    public void Reassign_SamePerson_IsIdempotent()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        delegation.ClearDomainEvents();

        delegation.Reassign(PersonId);

        Assert.Empty(delegation.DomainEvents);
        Assert.Equal(PersonId, delegation.DelegatePersonId);
    }

    // 2.8 Test Reprioritize and UpdateDueDate
    [Fact]
    public void Reprioritize_ChangesPriorityAndRaisesEvent()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        delegation.ClearDomainEvents();

        delegation.Reprioritize(Priority.Urgent);

        Assert.Equal(Priority.Urgent, delegation.Priority);
        var domainEvent = Assert.Single(delegation.DomainEvents);
        var reprioritized = Assert.IsType<DelegationReprioritized>(domainEvent);
        Assert.Equal(Priority.Urgent, reprioritized.NewPriority);
    }

    [Fact]
    public void UpdateDueDate_SetsNewDateAndRaisesEvent()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId);
        delegation.ClearDomainEvents();
        var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));

        delegation.UpdateDueDate(newDate);

        Assert.Equal(newDate, delegation.DueDate);
        var domainEvent = Assert.Single(delegation.DomainEvents);
        var changed = Assert.IsType<DelegationDueDateChanged>(domainEvent);
        Assert.Equal(newDate, changed.NewDueDate);
    }

    [Fact]
    public void UpdateDueDate_ClearsDateAndRaisesEvent()
    {
        var delegation = Delegation.Create(UserId, "Test", PersonId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)));
        delegation.ClearDomainEvents();

        delegation.UpdateDueDate(null);

        Assert.Null(delegation.DueDate);
        var domainEvent = Assert.Single(delegation.DomainEvents);
        var changed = Assert.IsType<DelegationDueDateChanged>(domainEvent);
        Assert.Null(changed.NewDueDate);
    }
}
