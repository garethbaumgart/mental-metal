namespace MentalMetal.Application.Common.Ai;

/// <summary>
/// Helpers for parsing AI provider responses that are expected to contain JSON.
/// Some providers (notably Anthropic's Claude models) frequently wrap structured
/// JSON output in a markdown code fence (<c>```json ... ```</c>) even when the
/// system prompt explicitly forbids it. Callers should run provider output through
/// <see cref="StripCodeFences"/> before handing it to <c>System.Text.Json</c>.
/// </summary>
public static class JsonResponseParser
{
    /// <summary>
    /// If <paramref name="raw"/> is wrapped in a markdown code fence, remove it.
    /// The language tag (e.g. <c>json</c>) is optional and matched case-insensitively.
    /// Un-fenced input is returned trimmed but otherwise unchanged.
    /// </summary>
    /// <param name="raw">Raw AI response content.</param>
    /// <returns>Content with any surrounding code fence removed and trimmed.</returns>
    public static string StripCodeFences(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return raw ?? string.Empty;

        var trimmed = raw.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
            return trimmed;

        // Opening fence: drop everything up to and including the first newline.
        // This handles "```json\n...", "```JSON\n...", "```\n...", and "```   \n...".
        var firstNewline = trimmed.IndexOf('\n');
        if (firstNewline < 0)
        {
            // Single line starting with ``` — degenerate; give up and return as-is trimmed
            // so the downstream JSON parser surfaces the real error.
            return trimmed;
        }

        var inner = trimmed[(firstNewline + 1)..];

        // Closing fence: strip trailing ``` if present (tolerating trailing whitespace/newlines).
        var innerTrimmed = inner.TrimEnd();
        if (innerTrimmed.EndsWith("```", StringComparison.Ordinal))
            inner = innerTrimmed[..^3];

        return inner.Trim();
    }
}
