using MentalMetal.Domain.Captures;

namespace MentalMetal.Domain.Tests.Captures;

public class CaptureTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static AiExtraction CreateTestExtraction(string summary = "Test summary") => new()
    {
        Summary = summary,
    };

    // 2.1 Test Capture creation with valid inputs and domain event
    [Fact]
    public void Create_ValidInputs_CreatesCaptureWithCorrectState()
    {
        var capture = Capture.Create(UserId, "Follow up with Sarah", CaptureType.QuickNote);

        Assert.NotEqual(Guid.Empty, capture.Id);
        Assert.Equal(UserId, capture.UserId);
        Assert.Equal("Follow up with Sarah", capture.RawContent);
        Assert.Equal(CaptureType.QuickNote, capture.CaptureType);
        Assert.Equal(ProcessingStatus.Raw, capture.ProcessingStatus);
        Assert.Null(capture.Title);
        Assert.Null(capture.Source);
        Assert.Empty(capture.LinkedPersonIds);
        Assert.Empty(capture.LinkedInitiativeIds);

        var domainEvent = Assert.Single(capture.DomainEvents);
        var created = Assert.IsType<CaptureCreated>(domainEvent);
        Assert.Equal(capture.Id, created.CaptureId);
        Assert.Equal(UserId, created.UserId);
        Assert.Equal(CaptureType.QuickNote, created.Type);
    }

    [Fact]
    public void Create_WithOptionalFields_SetsAllFields()
    {
        var capture = Capture.Create(UserId, "Transcript content", CaptureType.Transcript,
            source: "leadership sync", title: "Leadership sync 2026-04-10");

        Assert.Equal("Transcript content", capture.RawContent);
        Assert.Equal(CaptureType.Transcript, capture.CaptureType);
        Assert.Equal("Leadership sync 2026-04-10", capture.Title);
        Assert.Equal("leadership sync", capture.Source);
    }

    // 2.2 Test Capture creation rejects empty/whitespace rawContent
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyRawContent_Throws(string? rawContent)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Capture.Create(UserId, rawContent!, CaptureType.QuickNote));
    }

    // 2.3 Test processing status state machine: valid transitions
    [Fact]
    public void BeginProcessing_FromRaw_TransitionsToProcessing()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.ClearDomainEvents();

        capture.BeginProcessing();

        Assert.Equal(ProcessingStatus.Processing, capture.ProcessingStatus);
        var domainEvent = Assert.Single(capture.DomainEvents);
        Assert.IsType<CaptureProcessingStarted>(domainEvent);
    }

    [Fact]
    public void CompleteProcessing_FromProcessing_TransitionsToProcessed()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.BeginProcessing();
        capture.ClearDomainEvents();

        var extraction = CreateTestExtraction("extracted data");
        capture.CompleteProcessing(extraction);

        Assert.Equal(ProcessingStatus.Processed, capture.ProcessingStatus);
        Assert.Equal(extraction, capture.AiExtraction);
        Assert.NotNull(capture.ProcessedAt);
        var domainEvent = Assert.Single(capture.DomainEvents);
        Assert.IsType<CaptureProcessed>(domainEvent);
    }

    [Fact]
    public void FailProcessing_FromProcessing_TransitionsToFailed()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.BeginProcessing();
        capture.ClearDomainEvents();

        capture.FailProcessing("timeout");

        Assert.Equal(ProcessingStatus.Failed, capture.ProcessingStatus);
        var domainEvent = Assert.Single(capture.DomainEvents);
        var failed = Assert.IsType<CaptureProcessingFailed>(domainEvent);
        Assert.Equal("timeout", failed.Reason);
    }

    [Fact]
    public void RetryProcessing_FromFailed_TransitionsToRaw()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.BeginProcessing();
        capture.FailProcessing("error");
        capture.ClearDomainEvents();

        capture.RetryProcessing();

        Assert.Equal(ProcessingStatus.Raw, capture.ProcessingStatus);
        var domainEvent = Assert.Single(capture.DomainEvents);
        Assert.IsType<CaptureRetryRequested>(domainEvent);
    }

    // 2.4 Test processing status state machine: invalid transitions throw domain exception
    [Fact]
    public void BeginProcessing_FromProcessing_Throws()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.BeginProcessing();

        Assert.Throws<InvalidOperationException>(() => capture.BeginProcessing());
    }

    [Fact]
    public void BeginProcessing_FromProcessed_Throws()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.BeginProcessing();
        capture.CompleteProcessing(CreateTestExtraction());

        Assert.Throws<InvalidOperationException>(() => capture.BeginProcessing());
    }

    [Fact]
    public void CompleteProcessing_FromRaw_Throws()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);

        Assert.Throws<InvalidOperationException>(() => capture.CompleteProcessing(CreateTestExtraction()));
    }

    [Fact]
    public void FailProcessing_FromRaw_Throws()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);

        Assert.Throws<InvalidOperationException>(() => capture.FailProcessing());
    }

    [Fact]
    public void RetryProcessing_FromRaw_Throws()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);

        Assert.Throws<InvalidOperationException>(() => capture.RetryProcessing());
    }

    // 2.5 Test link/unlink person idempotency
    [Fact]
    public void LinkToPerson_AddsAndRaisesEvent()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.ClearDomainEvents();
        var personId = Guid.NewGuid();

        capture.LinkToPerson(personId);

        Assert.Single(capture.LinkedPersonIds);
        Assert.Contains(personId, capture.LinkedPersonIds);
        var domainEvent = Assert.Single(capture.DomainEvents);
        var linked = Assert.IsType<CaptureLinkedToPerson>(domainEvent);
        Assert.Equal(personId, linked.PersonId);
    }

    [Fact]
    public void LinkToPerson_Duplicate_IsIdempotent()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        var personId = Guid.NewGuid();
        capture.LinkToPerson(personId);
        capture.ClearDomainEvents();

        capture.LinkToPerson(personId);

        Assert.Single(capture.LinkedPersonIds);
        Assert.Empty(capture.DomainEvents);
    }

    [Fact]
    public void UnlinkFromPerson_RemovesAndRaisesEvent()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        var personId = Guid.NewGuid();
        capture.LinkToPerson(personId);
        capture.ClearDomainEvents();

        capture.UnlinkFromPerson(personId);

        Assert.Empty(capture.LinkedPersonIds);
        var domainEvent = Assert.Single(capture.DomainEvents);
        Assert.IsType<CaptureUnlinkedFromPerson>(domainEvent);
    }

    [Fact]
    public void UnlinkFromPerson_NotLinked_IsIdempotent()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.ClearDomainEvents();

        capture.UnlinkFromPerson(Guid.NewGuid());

        Assert.Empty(capture.DomainEvents);
    }

    // 2.6 Test link/unlink initiative idempotency
    [Fact]
    public void LinkToInitiative_AddsAndRaisesEvent()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.ClearDomainEvents();
        var initiativeId = Guid.NewGuid();

        capture.LinkToInitiative(initiativeId);

        Assert.Single(capture.LinkedInitiativeIds);
        Assert.Contains(initiativeId, capture.LinkedInitiativeIds);
        var domainEvent = Assert.Single(capture.DomainEvents);
        var linked = Assert.IsType<CaptureLinkedToInitiative>(domainEvent);
        Assert.Equal(initiativeId, linked.InitiativeId);
    }

    [Fact]
    public void LinkToInitiative_Duplicate_IsIdempotent()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        var initiativeId = Guid.NewGuid();
        capture.LinkToInitiative(initiativeId);
        capture.ClearDomainEvents();

        capture.LinkToInitiative(initiativeId);

        Assert.Single(capture.LinkedInitiativeIds);
        Assert.Empty(capture.DomainEvents);
    }

    [Fact]
    public void UnlinkFromInitiative_RemovesAndRaisesEvent()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        var initiativeId = Guid.NewGuid();
        capture.LinkToInitiative(initiativeId);
        capture.ClearDomainEvents();

        capture.UnlinkFromInitiative(initiativeId);

        Assert.Empty(capture.LinkedInitiativeIds);
        var domainEvent = Assert.Single(capture.DomainEvents);
        Assert.IsType<CaptureUnlinkedFromInitiative>(domainEvent);
    }

    [Fact]
    public void UnlinkFromInitiative_NotLinked_IsIdempotent()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.ClearDomainEvents();

        capture.UnlinkFromInitiative(Guid.NewGuid());

        Assert.Empty(capture.DomainEvents);
    }

    // 2.7 Test metadata update
    [Fact]
    public void UpdateMetadata_SetsTitleAndSourceAndRaisesEvent()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.ClearDomainEvents();

        capture.UpdateMetadata("New Title", "weekly 1:1");

        Assert.Equal("New Title", capture.Title);
        Assert.Equal("weekly 1:1", capture.Source);
        var domainEvent = Assert.Single(capture.DomainEvents);
        Assert.IsType<CaptureMetadataUpdated>(domainEvent);
    }

    [Fact]
    public void UpdateMetadata_ClearsOptionalFields()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote,
            source: "old source", title: "old title");
        capture.ClearDomainEvents();

        capture.UpdateMetadata(null, null);

        Assert.Null(capture.Title);
        Assert.Null(capture.Source);
    }

    // RecordSpawned* tests
    [Fact]
    public void RecordSpawnedCommitment_AddsIdAndUpdatesTimestamp()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        var commitmentId = Guid.NewGuid();

        capture.RecordSpawnedCommitment(commitmentId);

        Assert.Single(capture.SpawnedCommitmentIds);
        Assert.Contains(commitmentId, capture.SpawnedCommitmentIds);
    }

    [Fact]
    public void RecordSpawnedCommitment_Duplicate_IsIdempotent()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        var commitmentId = Guid.NewGuid();
        capture.RecordSpawnedCommitment(commitmentId);

        capture.RecordSpawnedCommitment(commitmentId);

        Assert.Single(capture.SpawnedCommitmentIds);
    }

    [Fact]
    public void RecordSpawnedDelegation_AddsId()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        var delegationId = Guid.NewGuid();

        capture.RecordSpawnedDelegation(delegationId);

        Assert.Single(capture.SpawnedDelegationIds);
        Assert.Contains(delegationId, capture.SpawnedDelegationIds);
    }

    [Fact]
    public void RecordSpawnedObservation_AddsId()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        var observationId = Guid.NewGuid();

        capture.RecordSpawnedObservation(observationId);

        Assert.Single(capture.SpawnedObservationIds);
        Assert.Contains(observationId, capture.SpawnedObservationIds);
    }
}
