using MentalMetal.Domain.Commitments;

namespace MentalMetal.Domain.Tests.Commitments;

public class CommitmentTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid PersonId = Guid.NewGuid();

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
        Assert.Equal(CommitmentConfidence.High, commitment.Confidence);
        Assert.Null(commitment.InitiativeId);
        Assert.Null(commitment.SourceCaptureId);
        Assert.Null(commitment.DueDate);
        Assert.Null(commitment.CompletedAt);
        Assert.Null(commitment.DismissedAt);
        Assert.Null(commitment.Notes);
    }

    [Fact]
    public void Create_WithConfidence_SetsConfidence()
    {
        var commitment = Commitment.Create(
            UserId, "Test", CommitmentDirection.MineToThem, PersonId,
            confidence: CommitmentConfidence.Medium);

        Assert.Equal(CommitmentConfidence.Medium, commitment.Confidence);
    }

    [Fact]
    public void Create_WithOptionalFields_SetsAllFields()
    {
        var initiativeId = Guid.NewGuid();
        var captureId = Guid.NewGuid();
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        var commitment = Commitment.Create(
            UserId, "Deliver design doc", CommitmentDirection.TheirsToMe, PersonId,
            dueDate, initiativeId, captureId, CommitmentConfidence.Low);

        Assert.Equal(CommitmentDirection.TheirsToMe, commitment.Direction);
        Assert.Equal(dueDate, commitment.DueDate);
        Assert.Equal(initiativeId, commitment.InitiativeId);
        Assert.Equal(captureId, commitment.SourceCaptureId);
        Assert.Equal(CommitmentConfidence.Low, commitment.Confidence);
    }

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

    // --- Complete ---

    [Fact]
    public void Complete_FromOpen_TransitionsToCompleted()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.ClearDomainEvents();

        commitment.Complete();

        Assert.Equal(CommitmentStatus.Completed, commitment.Status);
        Assert.NotNull(commitment.CompletedAt);
        var domainEvent = Assert.Single(commitment.DomainEvents);
        Assert.IsType<CommitmentCompleted>(domainEvent);
    }

    [Fact]
    public void Complete_FromDismissed_Throws()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.Dismiss();

        Assert.Throws<InvalidOperationException>(() => commitment.Complete());
    }

    [Fact]
    public void Complete_FromCompleted_Throws()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.Complete();

        Assert.Throws<InvalidOperationException>(() => commitment.Complete());
    }

    // --- Dismiss ---

    [Fact]
    public void Dismiss_FromOpen_TransitionsToDismissed()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.ClearDomainEvents();

        commitment.Dismiss();

        Assert.Equal(CommitmentStatus.Dismissed, commitment.Status);
        Assert.NotNull(commitment.DismissedAt);
        var domainEvent = Assert.Single(commitment.DomainEvents);
        Assert.IsType<CommitmentDismissed>(domainEvent);
    }

    [Fact]
    public void Dismiss_FromCompleted_Throws()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.Complete();

        Assert.Throws<InvalidOperationException>(() => commitment.Dismiss());
    }

    [Fact]
    public void Dismiss_AlreadyDismissed_IsIdempotent()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.Dismiss();
        commitment.ClearDomainEvents();

        commitment.Dismiss(); // should not throw

        Assert.Equal(CommitmentStatus.Dismissed, commitment.Status);
        Assert.Empty(commitment.DomainEvents);
    }

    // --- Reopen ---

    [Fact]
    public void Reopen_FromCompleted_TransitionsToOpen()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.Complete();
        commitment.ClearDomainEvents();

        commitment.Reopen();

        Assert.Equal(CommitmentStatus.Open, commitment.Status);
        Assert.Null(commitment.CompletedAt);
        Assert.Null(commitment.DismissedAt);
        var domainEvent = Assert.Single(commitment.DomainEvents);
        Assert.IsType<CommitmentReopened>(domainEvent);
    }

    [Fact]
    public void Reopen_FromDismissed_TransitionsToOpen()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.Dismiss();
        commitment.ClearDomainEvents();

        commitment.Reopen();

        Assert.Equal(CommitmentStatus.Open, commitment.Status);
        Assert.Null(commitment.DismissedAt);
    }

    [Fact]
    public void Reopen_FromCancelled_TransitionsToOpen()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        commitment.Cancel();
        commitment.ClearDomainEvents();

        commitment.Reopen();

        Assert.Equal(CommitmentStatus.Open, commitment.Status);
    }

    [Fact]
    public void Reopen_FromOpen_Throws()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);

        Assert.Throws<InvalidOperationException>(() => commitment.Reopen());
    }

    // --- IsOverdue ---

    [Fact]
    public void IsOverdue_OpenWithPastDueDate_ReturnsTrue()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));

        Assert.True(commitment.IsOverdue);
    }

    [Fact]
    public void IsOverdue_DismissedWithPastDueDate_ReturnsFalse()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        commitment.Dismiss();

        Assert.False(commitment.IsOverdue);
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

    // --- Cancel ---

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
    public void Complete_WithNotes_AppendsNotes()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);

        commitment.Complete("Delivered in leadership sync");

        Assert.Equal("Delivered in leadership sync", commitment.Notes);
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
    public void UpdateDirection_ChangesDirectionAndUpdatesTimestamp()
    {
        var commitment = Commitment.Create(UserId, "Test", CommitmentDirection.MineToThem, PersonId);
        var originalUpdatedAt = commitment.UpdatedAt;

        commitment.UpdateDirection(CommitmentDirection.TheirsToMe);

        Assert.Equal(CommitmentDirection.TheirsToMe, commitment.Direction);
        Assert.True(commitment.UpdatedAt >= originalUpdatedAt);
    }
}
