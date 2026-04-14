using MentalMetal.Application.Captures;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Captures;

namespace MentalMetal.Application.Tests.Captures;

public class TranscriptSegmentSplitterTests
{
    [Fact]
    public void Split_ShortSegment_ReturnsSingle()
    {
        var dto = new AudioTranscriptSegmentDto(1.0, 2.0, "Speaker A", "short");
        var result = InvokeSplit(new[] { dto });

        var segment = Assert.Single(result);
        Assert.Equal("short", segment.Text);
        Assert.Equal(1.0, segment.StartSeconds);
        Assert.Equal(2.0, segment.EndSeconds);
        Assert.Equal("Speaker A", segment.SpeakerLabel);
    }

    [Fact]
    public void Split_OverLengthSegment_SplitsIntoAdjacentSegmentsPreservingLabelAndTimeRange()
    {
        // 4500 chars, 60s–120s → 3 chunks (2000 + 2000 + 500)
        var text = string.Concat(Enumerable.Repeat("a", 2000))
                 + string.Concat(Enumerable.Repeat("b", 2000))
                 + string.Concat(Enumerable.Repeat("c", 500));

        var dto = new AudioTranscriptSegmentDto(60.0, 120.0, "Speaker A", text);
        var result = InvokeSplit(new[] { dto });

        Assert.Equal(3, result.Count);
        Assert.All(result, s => Assert.Equal("Speaker A", s.SpeakerLabel));
        Assert.All(result, s => Assert.True(s.Text.Length <= TranscriptSegment.MaxTextLength));

        // Concatenated text should equal original
        Assert.Equal(text, string.Concat(result.Select(r => r.Text)));

        // Adjacent — no gaps, no overlaps
        Assert.Equal(60.0, result[0].StartSeconds);
        Assert.Equal(result[0].EndSeconds, result[1].StartSeconds);
        Assert.Equal(result[1].EndSeconds, result[2].StartSeconds);
        Assert.Equal(120.0, result[2].EndSeconds);

        // Proportional allocation — first two chunks equal length (2000) should take equal duration
        var d0 = result[0].EndSeconds - result[0].StartSeconds;
        var d1 = result[1].EndSeconds - result[1].StartSeconds;
        Assert.Equal(d0, d1, 3);
    }

    [Fact]
    public void Split_ExactlyMaxLength_NotSplit()
    {
        var text = new string('x', TranscriptSegment.MaxTextLength);
        var dto = new AudioTranscriptSegmentDto(0, 10, "S", text);
        var result = InvokeSplit(new[] { dto });

        var segment = Assert.Single(result);
        Assert.Equal(text, segment.Text);
    }

    private static IReadOnlyList<TranscriptSegment> InvokeSplit(
        IEnumerable<AudioTranscriptSegmentDto> dtos) => TranscriptSegmentSplitter.Split(dtos);
}
