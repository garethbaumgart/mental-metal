using MentalMetal.Domain.Commitments;

namespace MentalMetal.Domain.Tests.Commitments;

public class CommitmentTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid PersonId = Guid.NewGuid();

    // 2.1 Test Commitment creation with valid inputs and domain event
    [Fact]
    public void Create_ValidInputs_CreatesCommitmentWithCorrectState()
    {
        var commitment = Commitment.Create(UserId, "Send Q3 roadmap draft", CommitmentDirection.MineToThem, PersonId);

        Assert.NotEqual(Guid.Empty, commitment.Id);
        Assert.Equal(UserId, commitment.UserId);
        Assert.Equal("Send Q3 roadmap draft", commitment.Description);
        Assert.Equal(CommitmentDirection.MineToThem, commitment.Direction);
        Assert.Equal(PersonId, commitment.PersonId);
        Assert.Equal(CommitmentStatus.Open, commitment.Status);
        Assert.Null(commitment.InitiativeId);
        Assert.Null(commitment.SourceCaptureId);
        Assert.Null(commitment.DueDate);
        Assert.Null(commitment.CompletedAt);
        Assert.Null(commitment.Notes);

        var domainEvent = Assert.Single(commitment.DomainEvents);
        var created = Assert.IsType<CommitmentCreated>(domainEvent);
        Assert.Equal(commitment.Id, created.CommitmentId);
        Assert.Equal(UserId, created.UserId);
        Assert.Equal(CommitmentDirection.MineToThem, created.Direction);
        Assert.Equal(PersonId, created.PersonId);
    }

    [Fact]
    public void Create_WithOptionalFields_SetsAllFields()
    {
        var initiativeId = Guid.NewGuid();
        var captureId = Guid.NewGuid();
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        var commitment = Commitment.Create(
            UserId, "Deliver design doc", CommitmentDirection.TheirsToMe, PersonId,
            dueDate, initiativeId, captureId);

        Assert.Equal(CommitmentDirection.TheirsToMe, commitment.Direction);
        Assert.Equal(dueDate, commitment.DueDate);
        Assert.Equal(initiativeId, commitment.InitiativeId);
        Assert.Equal(captureId, commitment.SourceCaptureId);
    }

    // 2.2 Test creation rejects empty description and missing personId
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyDescription_Throws(string? description)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Commitment.Create(UserId, description!, CommitmentDirection.MineToThem, PersonId));
    }

    [Fact]
    public void Create_EmptyPersonId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, Guid.Empty));
    }

    // 2.3 Test status transitions: Open->Completed, Open->Cancelled, Completed->Open, Cancelled->Open
    [Fact]
    public void Complete_FromOpen_TransitionsToCompleted()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.ClearDomainEvents();

        commitment.Complete();

        Assert.Equal(CommitmentStatus.Completed, commitment.Status);
        var domainEvent = Assert.Single(commitment.DomainEvents);
        Assert.IsType<CommitmentCompleted>(domainEvent);
    }

    [Fact]
    public void Cancel_FromOpen_TransitionsToCancelled()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.ClearDomainEvents();

        commitment.Cancel();

        Assert.Equal(CommitmentStatus.Cancelled, commitment.Status);
        var domainEvent = Assert.Single(commitment.DomainEvents);
        Assert.IsType<CommitmentCancelled>(domainEvent);
    }

    [Fact]
    public void Reopen_FromCompleted_TransitionsToOpen()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.Complete();
        commitment.ClearDomainEvents();

        commitment.Reopen();

        Assert.Equal(CommitmentStatus.Open, commitment.Status);
        var domainEvent = Assert.Single(commitment.DomainEvents);
        Assert.IsType<CommitmentReopened>(domainEvent);
    }

    [Fact]
    public void Reopen_FromCancelled_TransitionsToOpen()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.Cancel();
        commitment.ClearDomainEvents();

        commitment.Reopen();

        Assert.Equal(CommitmentStatus.Open, commitment.Status);
        var domainEvent = Assert.Single(commitment.DomainEvents);
        Assert.IsType<CommitmentReopened>(domainEvent);
    }

    // 2.4 Test invalid status transitions throw domain exception
    [Fact]
    public void Complete_FromCompleted_Throws()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.Complete();

        Assert.Throws<InvalidOperationException>(() => commitment.Complete());
    }

    [Fact]
    public void Complete_FromCancelled_Throws()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.Cancel();

        Assert.Throws<InvalidOperationException>(() => commitment.Complete());
    }

    [Fact]
    public void Cancel_FromCancelled_Throws()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.Cancel();

        Assert.Throws<InvalidOperationException>(() => commitment.Cancel());
    }

    [Fact]
    public void Cancel_FromCompleted_Throws()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.Complete();

        Assert.Throws<InvalidOperationException>(() => commitment.Cancel());
    }

    [Fact]
    public void Reopen_FromOpen_Throws()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);

        Assert.Throws<InvalidOperationException>(() => commitment.Reopen());
    }

    // 2.5 Test CompletedAt is set on completion and cleared on reopen
    [Fact]
    public void Complete_SetsCompletedAt()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);

        commitment.Complete();

        Assert.NotNull(commitment.CompletedAt);
    }

    [Fact]
    public void Reopen_ClearsCompletedAt()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.Complete();
        Assert.NotNull(commitment.CompletedAt);

        commitment.Reopen();

        Assert.Null(commitment.CompletedAt);
    }

    // 2.6 Test IsOverdue computation for all status/date combinations
    [Fact]
    public void IsOverdue_OpenWithPastDueDate_ReturnsTrue()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));

        Assert.True(commitment.IsOverdue);
    }

    [Fact]
    public void IsOverdue_OpenWithFutureDueDate_ReturnsFalse()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)));

        Assert.False(commitment.IsOverdue);
    }

    [Fact]
    public void IsOverdue_OpenWithNoDueDate_ReturnsFalse()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);

        Assert.False(commitment.IsOverdue);
    }

    [Fact]
    public void IsOverdue_CompletedWithPastDueDate_ReturnsFalse()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        commitment.Complete();

        Assert.False(commitment.IsOverdue);
    }

    [Fact]
    public void IsOverdue_CancelledWithPastDueDate_ReturnsFalse()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        commitment.Cancel();

        Assert.False(commitment.IsOverdue);
    }

    // 2.7 Test MarkOverdue raises event only when conditions met
    [Fact]
    public void MarkOverdue_WhenOverdue_RaisesEvent()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        commitment.ClearDomainEvents();

        commitment.MarkOverdue();

        var domainEvent = Assert.Single(commitment.DomainEvents);
        Assert.IsType<CommitmentBecameOverdue>(domainEvent);
    }

    [Fact]
    public void MarkOverdue_WhenNotOverdue_DoesNotRaiseEvent()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)));
        commitment.ClearDomainEvents();

        commitment.MarkOverdue();

        Assert.Empty(commitment.DomainEvents);
    }

    [Fact]
    public void MarkOverdue_WhenCompleted_DoesNotRaiseEvent()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        commitment.Complete();
        commitment.ClearDomainEvents();

        commitment.MarkOverdue();

        Assert.Empty(commitment.DomainEvents);
    }

    // 2.8 Test UpdateDueDate and UpdateDescription
    [Fact]
    public void UpdateDueDate_SetsNewDateAndRaisesEvent()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.ClearDomainEvents();
        var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));

        commitment.UpdateDueDate(newDate);

        Assert.Equal(newDate, commitment.DueDate);
        var domainEvent = Assert.Single(commitment.DomainEvents);
        var changed = Assert.IsType<CommitmentDueDateChanged>(domainEvent);
        Assert.Equal(newDate, changed.NewDueDate);
    }

    [Fact]
    public void UpdateDueDate_ClearsDateAndRaisesEvent()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)));
        commitment.ClearDomainEvents();

        commitment.UpdateDueDate(null);

        Assert.Null(commitment.DueDate);
        var domainEvent = Assert.Single(commitment.DomainEvents);
        var changed = Assert.IsType<CommitmentDueDateChanged>(domainEvent);
        Assert.Null(changed.NewDueDate);
    }

    [Fact]
    public void UpdateDescription_SetsNewDescriptionAndRaisesEvent()
    {
        var commitment = Commitment.Create(UserId, "Old description", CommitmentDirection.MineToThem, PersonId);
        commitment.ClearDomainEvents();

        commitment.UpdateDescription("New description");

        Assert.Equal("New description", commitment.Description);
        var domainEvent = Assert.Single(commitment.DomainEvents);
        Assert.IsType<CommitmentDescriptionUpdated>(domainEvent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void UpdateDescription_EmptyDescription_Throws(string? description)
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);

        Assert.ThrowsAny<ArgumentException>(() => commitment.UpdateDescription(description!));
    }

    [Fact]
    public void LinkToInitiative_SetsInitiativeIdAndRaisesEvent()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.ClearDomainEvents();
        var initiativeId = Guid.NewGuid();

        commitment.LinkToInitiative(initiativeId);

        Assert.Equal(initiativeId, commitment.InitiativeId);
        var domainEvent = Assert.Single(commitment.DomainEvents);
        var linked = Assert.IsType<CommitmentLinkedToInitiative>(domainEvent);
        Assert.Equal(initiativeId, linked.InitiativeId);
    }

    [Fact]
    public void LinkToInitiative_SameInitiative_IsIdempotent()
    {
        var initiativeId = Guid.NewGuid();
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId,
            initiativeId: initiativeId);
        commitment.ClearDomainEvents();

        commitment.LinkToInitiative(initiativeId);

        Assert.Empty(commitment.DomainEvents);
    }

    [Fact]
    public void Complete_WithNotes_AppendsNotes()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);

        commitment.Complete("Delivered in leadership sync");

        Assert.Equal("Delivered in leadership sync", commitment.Notes);
    }

    [Fact]
    public void Cancel_WithReason_AppendsReason()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);

        commitment.Cancel("No longer relevant after reorg");

        Assert.Equal("No longer relevant after reorg", commitment.Notes);
    }
}
