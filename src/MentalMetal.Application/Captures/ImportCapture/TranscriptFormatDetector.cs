using System.Text.RegularExpressions;

namespace MentalMetal.Application.Captures.ImportCapture;

public sealed record FormatDetectionResult(string NormalizedContent, bool IsMeetFormat);

public static partial class TranscriptFormatDetector
{
    [GeneratedRegex(@"^\p{Lu}[\p{L} '\u2019\-]{1,40}:\s", RegexOptions.Multiline)]
    private static partial Regex SpeakerPattern();

    [GeneratedRegex(@"^(Summary|Transcript)\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex SectionHeadingPattern();

    public static FormatDetectionResult Detect(string content)
    {
        ArgumentNullException.ThrowIfNull(content, nameof(content));

        // Normalize line endings
        var normalized = content
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');

        var hasSectionHeading = SectionHeadingPattern().IsMatch(normalized);
        var speakerMatches = SpeakerPattern().Matches(normalized);
        var hasSpeakerTurns = speakerMatches.Count >= 2;

        if (!hasSectionHeading && !hasSpeakerTurns)
            return new FormatDetectionResult(normalized, false);

        // Normalize whitespace runs between speaker turns but preserve the turns themselves
        var lines = normalized.Split('\n');
        var cleanedLines = new List<string>(lines.Length);
        var lastWasBlank = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimEnd();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                if (!lastWasBlank)
                    cleanedLines.Add("");
                lastWasBlank = true;
            }
            else
            {
                cleanedLines.Add(trimmed);
                lastWasBlank = false;
            }
        }

        return new FormatDetectionResult(
            string.Join('\n', cleanedLines).Trim(),
            true);
    }
}
