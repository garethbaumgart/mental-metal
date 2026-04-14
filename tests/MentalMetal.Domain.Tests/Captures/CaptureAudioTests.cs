using MentalMetal.Domain.Captures;

namespace MentalMetal.Domain.Tests.Captures;

public class CaptureAudioTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 4, 14, 12, 0, 0, TimeSpan.Zero);

    private static Capture CreateAudio() =>
        Capture.CreateAudio(UserId, "blob/ref.webm", "audio/webm", 42.5, Now);

    [Fact]
    public void CreateAudio_ValidInputs_StartsPendingAndRaisesEvents()
    {
        var capture = CreateAudio();

        Assert.Equal(CaptureType.AudioRecording, capture.CaptureType);
        Assert.Equal("blob/ref.webm", capture.AudioBlobRef);
        Assert.Equal("audio/webm", capture.AudioMimeType);
        Assert.Equal(42.5, capture.AudioDurationSeconds);
        Assert.Equal(TranscriptionStatus.Pending, capture.TranscriptionStatus);
        Assert.Equal(ProcessingStatus.Raw, capture.ProcessingStatus);
        Assert.Null(capture.AudioDiscardedAt);

        Assert.Collection(capture.DomainEvents,
            e => Assert.IsType<CaptureCreated>(e),
            e => Assert.IsType<CaptureAudioUploaded>(e));
    }

    [Fact]
    public void CreateAudio_EmptyBlobRef_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Capture.CreateAudio(UserId, "", "audio/webm", 1.0, Now));
    }

    [Fact]
    public void CreateAudio_NegativeDuration_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Capture.CreateAudio(UserId, "ref", "audio/webm", -1, Now));
    }

    [Fact]
    public void BeginTranscription_FromPending_TransitionsToInProgress()
    {
        var c = CreateAudio();
        c.BeginTranscription(Now);
        Assert.Equal(TranscriptionStatus.InProgress, c.TranscriptionStatus);
    }

    [Fact]
    public void BeginTranscription_FromNonPending_Throws()
    {
        var c = CreateAudio();
        c.BeginTranscription(Now);
        Assert.Throws<InvalidOperationException>(() => c.BeginTranscription(Now));
    }

    [Fact]
    public void AttachTranscript_FromInProgress_Transcribes_RaisesEvent_LeavesProcessingAtRaw()
    {
        var c = CreateAudio();
        c.BeginTranscription(Now);
        c.ClearDomainEvents();

        var segments = new[]
        {
            TranscriptSegment.Create(0, 5, "Speaker A", "Hello world"),
            TranscriptSegment.Create(5, 10, "Speaker B", "Hi there")
        };

        c.AttachTranscript("Hello world\nHi there", segments, Now);

        Assert.Equal(TranscriptionStatus.Transcribed, c.TranscriptionStatus);
        Assert.Equal(ProcessingStatus.Raw, c.ProcessingStatus); // extraction pipeline still picks up
        Assert.Equal("Hello world\nHi there", c.RawContent);
        Assert.Equal(2, c.TranscriptSegments.Count);

        var evt = Assert.Single(c.DomainEvents);
        var transcribed = Assert.IsType<CaptureTranscribed>(evt);
        Assert.Equal(2, transcribed.SegmentCount);
    }

    [Fact]
    public void AttachTranscript_FromPending_Throws()
    {
        var c = CreateAudio();
        Assert.Throws<InvalidOperationException>(() =>
            c.AttachTranscript("text", Array.Empty<TranscriptSegment>(), Now));
    }

    [Fact]
    public void MarkTranscriptionFailed_FromInProgress_TransitionsToFailed_LeavesProcessingUntouched()
    {
        var c = CreateAudio();
        c.BeginTranscription(Now);
        c.ClearDomainEvents();

        c.MarkTranscriptionFailed("provider timeout", Now);

        Assert.Equal(TranscriptionStatus.Failed, c.TranscriptionStatus);
        Assert.Equal("provider timeout", c.TranscriptionFailureReason);
        Assert.Equal(ProcessingStatus.Raw, c.ProcessingStatus);

        var evt = Assert.Single(c.DomainEvents);
        var failed = Assert.IsType<CaptureTranscriptionFailed>(evt);
        Assert.Equal("provider timeout", failed.Reason);
    }

    [Fact]
    public void MarkTranscriptionFailed_FromPending_Throws()
    {
        var c = CreateAudio();
        Assert.Throws<InvalidOperationException>(() => c.MarkTranscriptionFailed(null, Now));
    }

    [Fact]
    public void RequeueTranscription_FromFailed_TransitionsToPending()
    {
        var c = CreateAudio();
        c.BeginTranscription(Now);
        c.MarkTranscriptionFailed("x", Now);

        c.RequeueTranscription(Now);

        Assert.Equal(TranscriptionStatus.Pending, c.TranscriptionStatus);
        Assert.Null(c.TranscriptionFailureReason);
    }

    [Fact]
    public void RequeueTranscription_AfterDiscard_Throws()
    {
        var c = CreateAudio();
        c.BeginTranscription(Now);
        c.MarkTranscriptionFailed("x", Now);
        // simulate blob deletion
        c.MarkAudioDiscarded(Now);

        Assert.Throws<InvalidOperationException>(() => c.RequeueTranscription(Now));
    }

    [Fact]
    public void MarkAudioDiscarded_ClearsBlobRefAndSetsTimestamp_IsIdempotent()
    {
        var c = CreateAudio();
        c.BeginTranscription(Now);
        c.AttachTranscript("t", new[] { TranscriptSegment.Create(0, 1, "A", "t") }, Now);

        c.MarkAudioDiscarded(Now);

        Assert.Null(c.AudioBlobRef);
        Assert.Equal(Now, c.AudioDiscardedAt);
        Assert.Equal("audio/webm", c.AudioMimeType); // retained
        Assert.Equal(42.5, c.AudioDurationSeconds);  // retained
        Assert.Contains(c.DomainEvents, e => e is CaptureAudioDiscarded);

        c.ClearDomainEvents();
        c.MarkAudioDiscarded(Now);
        Assert.Empty(c.DomainEvents);
    }

    [Fact]
    public void IdentifySpeakers_MapsLabelsToPersons_RaisesEventPerDistinctMapping()
    {
        var c = CreateAudio();
        c.BeginTranscription(Now);
        var segments = new[]
        {
            TranscriptSegment.Create(0, 5, "Speaker A", "hi"),
            TranscriptSegment.Create(5, 10, "Speaker B", "hello"),
            TranscriptSegment.Create(10, 15, "Speaker A", "bye"),
        };
        c.AttachTranscript("x", segments, Now);
        c.ClearDomainEvents();

        var sarah = Guid.NewGuid();
        var tom = Guid.NewGuid();
        var mapping = new Dictionary<string, Guid>
        {
            { "Speaker A", sarah },
            { "Speaker B", tom }
        };

        c.IdentifySpeakers(mapping, Now);

        Assert.All(c.TranscriptSegments.Where(s => s.SpeakerLabel == "Speaker A"),
            s => Assert.Equal(sarah, s.LinkedPersonId));
        Assert.All(c.TranscriptSegments.Where(s => s.SpeakerLabel == "Speaker B"),
            s => Assert.Equal(tom, s.LinkedPersonId));

        Assert.Equal(2, c.DomainEvents.OfType<CaptureSpeakerIdentified>().Count());
    }

    [Fact]
    public void IdentifySpeakers_UnknownLabel_Throws()
    {
        var c = CreateAudio();
        c.BeginTranscription(Now);
        c.AttachTranscript("x", new[] { TranscriptSegment.Create(0, 1, "Speaker A", "hi") }, Now);

        var mapping = new Dictionary<string, Guid> { { "Speaker Z", Guid.NewGuid() } };

        Assert.Throws<KeyNotFoundException>(() => c.IdentifySpeakers(mapping, Now));
    }

    [Fact]
    public void IdentifySpeakers_EmptyMapping_IsNoOp()
    {
        var c = CreateAudio();
        c.BeginTranscription(Now);
        c.AttachTranscript("x", new[] { TranscriptSegment.Create(0, 1, "Speaker A", "hi") }, Now);
        c.ClearDomainEvents();

        c.IdentifySpeakers(new Dictionary<string, Guid>(), Now);

        Assert.Empty(c.DomainEvents);
    }

    // ----- TranscriptSegment invariants -----

    [Fact]
    public void TranscriptSegment_Create_EmptyText_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            TranscriptSegment.Create(0, 1, "Speaker A", ""));
    }

    [Fact]
    public void TranscriptSegment_Create_EndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TranscriptSegment.Create(5, 3, "Speaker A", "text"));
    }

    [Fact]
    public void TranscriptSegment_Create_OverLengthText_Throws()
    {
        var text = new string('a', TranscriptSegment.MaxTextLength + 1);
        Assert.Throws<ArgumentException>(() =>
            TranscriptSegment.Create(0, 1, "Speaker A", text));
    }

    [Fact]
    public void TranscriptSegment_Create_OverLengthLabel_Throws()
    {
        var label = new string('x', TranscriptSegment.MaxSpeakerLabelLength + 1);
        Assert.Throws<ArgumentException>(() =>
            TranscriptSegment.Create(0, 1, label, "text"));
    }

    [Fact]
    public void TranscriptSegment_Create_NegativeStart_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TranscriptSegment.Create(-1, 1, "A", "text"));
    }
}
