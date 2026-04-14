using MentalMetal.Application.Common.Ai;

namespace MentalMetal.Infrastructure.Ai;

/// <summary>
/// Dev/test-only stub for <see cref="IAudioTranscriptionProvider"/>.
/// Produces deterministic fake output (two speakers, fixed boilerplate text).
/// MUST NEVER be registered in production — the DI wiring in
/// <c>DependencyInjection</c> only adds this in development environments.
/// </summary>
public sealed class StubAudioTranscriptionProvider : IAudioTranscriptionProvider
{
    public Task<AudioTranscriptionResult> TranscribeAsync(
        AudioTranscriptionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var duration = Math.Max(request.DurationSeconds, 1.0);
        var mid = Math.Round(duration / 2.0, 2);

        var segments = new List<AudioTranscriptSegmentDto>
        {
            new(0.0, mid, "Speaker A", "Stub transcript segment from Speaker A."),
            new(mid, duration, "Speaker B", "Stub transcript segment from Speaker B."),
        };

        var fullText = string.Join("\n", segments.Select(s => $"[{s.SpeakerLabel}] {s.Text}"));

        return Task.FromResult(new AudioTranscriptionResult(fullText, segments));
    }
}
