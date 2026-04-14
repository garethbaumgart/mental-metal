using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Captures;

namespace MentalMetal.Application.Captures;

/// <summary>
/// Splits provider-returned segments whose <c>Text</c> exceeds
/// <see cref="TranscriptSegment.MaxTextLength"/> into adjacent domain segments
/// with proportional time ranges, so no content is dropped and the domain
/// invariants hold. See capture-audio design D3.1.
/// </summary>
public static class TranscriptSegmentSplitter
{
    public static IReadOnlyList<TranscriptSegment> Split(
        IEnumerable<AudioTranscriptSegmentDto> providerSegments)
    {
        ArgumentNullException.ThrowIfNull(providerSegments);

        var result = new List<TranscriptSegment>();
        foreach (var dto in providerSegments)
        {
            if (dto.Text.Length <= TranscriptSegment.MaxTextLength)
            {
                result.Add(TranscriptSegment.Create(
                    dto.StartSeconds, dto.EndSeconds, dto.SpeakerLabel, dto.Text));
                continue;
            }

            var chunks = ChunkByMaxLength(dto.Text, TranscriptSegment.MaxTextLength);
            var totalChars = dto.Text.Length;
            var duration = dto.EndSeconds - dto.StartSeconds;
            var consumedChars = 0;
            var cursor = dto.StartSeconds;

            for (var i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                consumedChars += chunk.Length;
                double segmentEnd;
                if (i == chunks.Count - 1)
                {
                    segmentEnd = dto.EndSeconds;
                }
                else
                {
                    segmentEnd = dto.StartSeconds + (duration * consumedChars / totalChars);
                    if (segmentEnd < cursor) segmentEnd = cursor;
                }

                result.Add(TranscriptSegment.Create(cursor, segmentEnd, dto.SpeakerLabel, chunk));
                cursor = segmentEnd;
            }
        }
        return result;
    }

    private static List<string> ChunkByMaxLength(string text, int max)
    {
        var chunks = new List<string>(capacity: (text.Length / max) + 1);
        for (var i = 0; i < text.Length; i += max)
        {
            var len = Math.Min(max, text.Length - i);
            chunks.Add(text.Substring(i, len));
        }
        return chunks;
    }
}
