namespace MentalMetal.Application.Briefings;

/// <summary>
/// Builds AI prompts for generating daily briefings.
/// </summary>
public static class DailyBriefPromptBuilder
{
    public const string SystemPrompt = """
        You are an executive intelligence assistant generating a daily briefing. Given yesterday's
        meeting transcripts, notes, and commitment data, create a concise morning brief.

        Your brief should cover:
        1. **Yesterday's Summary** — what happened across all meetings/notes (2-3 sentences)
        2. **Key Decisions** — concrete decisions that were made
        3. **Action Items** — commitments that emerged
        4. **Risks & Concerns** — anything flagged as a risk or blocker
        5. **People Highlights** — notable interactions or changes in key relationships

        Rules:
        - Be concise and action-oriented — this is a morning brief, not a novel
        - Use markdown formatting (headers, bullet points) for readability
        - Only reference information that appears in the provided data
        - Do NOT invent or hallucinate information
        - If there is nothing notable in a category, skip it entirely
        - Prioritize items that require immediate attention
        """;

    public static string BuildUserPrompt(
        IEnumerable<CaptureContextForBrief> captures,
        IEnumerable<CommitmentContextForBrief> dueToday,
        IEnumerable<CommitmentContextForBrief> overdue)
    {
        var lines = new List<string>
        {
            "## Yesterday's Captures"
        };

        var captureList = captures.ToList();
        if (captureList.Count == 0)
        {
            lines.Add("No captures from yesterday.");
        }
        else
        {
            foreach (var c in captureList)
            {
                lines.Add($"### {c.Title ?? "Untitled"} ({c.CapturedAt:yyyy-MM-dd HH:mm})");
                if (!string.IsNullOrWhiteSpace(c.Summary))
                    lines.Add($"**Summary:** {c.Summary}");
                if (c.Decisions.Count > 0)
                    lines.Add($"**Decisions:** {string.Join("; ", c.Decisions)}");
                if (c.Risks.Count > 0)
                    lines.Add($"**Risks:** {string.Join("; ", c.Risks)}");
                lines.Add("");
            }
        }

        lines.Add("## Commitments Due Today");
        var dueTodayList = dueToday.ToList();
        if (dueTodayList.Count == 0)
        {
            lines.Add("None.");
        }
        else
        {
            foreach (var c in dueTodayList)
            {
                var direction = c.Direction == "MineToThem" ? "I owe" : "They owe me";
                lines.Add($"- [{direction}] {c.Description} (person: {c.PersonName ?? "unknown"})");
            }
        }

        lines.Add("");
        lines.Add("## Overdue Commitments");
        var overdueList = overdue.ToList();
        if (overdueList.Count == 0)
        {
            lines.Add("None.");
        }
        else
        {
            foreach (var c in overdueList)
            {
                var direction = c.Direction == "MineToThem" ? "I owe" : "They owe me";
                lines.Add($"- [{direction}] {c.Description} (due: {c.DueDate}, person: {c.PersonName ?? "unknown"})");
            }
        }

        return string.Join("\n", lines);
    }
}

public sealed record CaptureContextForBrief(
    string? Title,
    DateTimeOffset CapturedAt,
    string? Summary,
    List<string> Decisions,
    List<string> Risks);

public sealed record CommitmentContextForBrief(
    string Description,
    string Direction,
    DateOnly? DueDate,
    string? PersonName);
