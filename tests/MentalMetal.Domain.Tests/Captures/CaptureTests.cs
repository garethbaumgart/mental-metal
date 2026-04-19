using MentalMetal.Domain.Captures;

namespace MentalMetal.Domain.Tests.Captures;

public class CaptureTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static AiExtraction CreateTestExtraction(string summary = "Test summary") => new()
    {
        Summary = summary,
        ExtractedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public void Create_ValidInputs_CreatesCaptureWithCorrectState()
    {
        var capture = Capture.Create(UserId, "Follow up with Alice", CaptureType.QuickNote);

        Assert.NotEqual(Guid.Empty, capture.Id);
        Assert.Equal(UserId, capture.UserId);
        Assert.Equal("Follow up with Alice", capture.RawContent);
        Assert.Equal(CaptureType.QuickNote, capture.CaptureType);
        Assert.Equal(ProcessingStatus.Raw, capture.ProcessingStatus);
        Assert.Null(capture.CaptureSource);
        Assert.Null(capture.Title);
        Assert.Empty(capture.LinkedPersonIds);
        Assert.Empty(capture.LinkedInitiativeIds);

        var domainEvent = Assert.Single(capture.DomainEvents);
        var created = Assert.IsType<CaptureCreated>(domainEvent);
        Assert.Equal(capture.Id, created.CaptureId);
    }

    [Fact]
    public void Create_WithSource_SetsCaptureSource()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.Transcript,
            source: CaptureSource.Upload, title: "Test");

        Assert.Equal(CaptureSource.Upload, capture.CaptureSource);
    }

    [Fact]
    public void Create_WithTypedSource_SetsCaptureSourceToTyped()
    {
        var capture = Capture.Create(UserId, "some typed note", CaptureType.QuickNote,
            source: CaptureSource.Typed);

        Assert.Equal(CaptureSource.Typed, capture.CaptureSource);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyRawContent_Throws(string? rawContent)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Capture.Create(UserId, rawContent!, CaptureType.QuickNote));
    }

    [Fact]
    public void BeginProcessing_FromRaw_TransitionsToProcessing()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.ClearDomainEvents();

        capture.BeginProcessing();

        Assert.Equal(ProcessingStatus.Processing, capture.ProcessingStatus);
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
    }

    [Fact]
    public void FailProcessing_FromProcessing_TransitionsToFailed()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.BeginProcessing();

        capture.FailProcessing("timeout");

        Assert.Equal(ProcessingStatus.Failed, capture.ProcessingStatus);
        Assert.Equal("timeout", capture.FailureReason);
    }

    [Fact]
    public void RetryProcessing_FromFailed_TransitionsToRaw()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.BeginProcessing();
        capture.FailProcessing("error");

        capture.RetryProcessing();

        Assert.Equal(ProcessingStatus.Raw, capture.ProcessingStatus);
        Assert.Null(capture.AiExtraction);
    }

    [Fact]
    public void BeginProcessing_FromProcessing_Throws()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.BeginProcessing();

        Assert.Throws<InvalidOperationException>(() => capture.BeginProcessing());
    }

    [Fact]
    public void RetryProcessing_FromRaw_Throws()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);

        Assert.Throws<InvalidOperationException>(() => capture.RetryProcessing());
    }

    [Fact]
    public void LinkToPerson_AddsAndRaisesEvent()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.ClearDomainEvents();
        var personId = Guid.NewGuid();

        capture.LinkToPerson(personId);

        Assert.Single(capture.LinkedPersonIds);
        Assert.Contains(personId, capture.LinkedPersonIds);
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
    public void LinkToInitiative_AddsAndRaisesEvent()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.ClearDomainEvents();
        var initiativeId = Guid.NewGuid();

        capture.LinkToInitiative(initiativeId);

        Assert.Single(capture.LinkedInitiativeIds);
        Assert.Contains(initiativeId, capture.LinkedInitiativeIds);
    }

    [Fact]
    public void UpdateMetadata_SetsTitleAndRaisesEvent()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);
        capture.ClearDomainEvents();

        capture.UpdateMetadata("New Title");

        Assert.Equal("New Title", capture.Title);
        var domainEvent = Assert.Single(capture.DomainEvents);
        Assert.IsType<CaptureMetadataUpdated>(domainEvent);
    }

    [Fact]
    public void RecordSpawnedCommitment_AddsId()
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
    public void Create_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Capture.Create(Guid.Empty, "content", CaptureType.QuickNote));
    }

    [Fact]
    public void LinkToPerson_EmptyPersonId_Throws()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);

        Assert.Throws<ArgumentException>(() => capture.LinkToPerson(Guid.Empty));
    }

    [Fact]
    public void LinkToInitiative_EmptyInitiativeId_Throws()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);

        Assert.Throws<ArgumentException>(() => capture.LinkToInitiative(Guid.Empty));
    }

    [Fact]
    public void RecordSpawnedCommitment_EmptyId_Throws()
    {
        var capture = Capture.Create(UserId, "content", CaptureType.QuickNote);

        Assert.Throws<ArgumentException>(() => capture.RecordSpawnedCommitment(Guid.Empty));
    }
}
