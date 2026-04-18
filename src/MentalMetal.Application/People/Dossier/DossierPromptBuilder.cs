namespace MentalMetal.Application.People.Dossier;

/// <summary>
/// Builds AI prompts for generating person dossiers in default and prep modes.
/// </summary>
public static class DossierPromptBuilder
{
    public static string SystemPrompt(string mode) => mode switch
    {
        "prep" => PrepSystemPrompt,
        _ => DefaultSystemPrompt
    };

    private const string DefaultSystemPrompt = """
        You are an executive intelligence assistant. Given information about a person gathered
        from meeting transcripts and notes, synthesize a concise dossier answering:
        "What should I know about this person right now?"

        Your synthesis should cover:
        1. **Current relationship status** — how interactions have been going recently
        2. **Open threads** — unfinished discussions, pending decisions, or follow-ups
        3. **Signals** — positive or negative sentiment shifts, engagement changes
        4. **Contradictions** — anything this person has said that conflicts with earlier statements
        5. **Key context** — role, responsibilities, and relevance to current work

        Rules:
        - Be concise but thorough — aim for 3-5 paragraphs
        - Use markdown formatting (headers, bullet points) for readability
        - Only reference information that appears in the provided data
        - Do NOT invent or hallucinate information
        - Highlight anything that requires attention or follow-up
        """;

    private const string PrepSystemPrompt = """
        You are an executive intelligence assistant preparing a pre-meeting brief. Given information
        about a person from recent meeting transcripts and notes, create a focused brief answering:
        "What do I need to know before meeting with this person?"

        Your brief should cover:
        1. **Recent interactions** — last few touchpoints and their outcomes
        2. **Open commitments** — what you owe them and what they owe you
        3. **Talking points** — suggested topics based on open threads and recent activity
        4. **Watch for** — contradictions, unresolved tensions, or things they may raise
        5. **Relationship temperature** — overall sentiment and engagement level

        Rules:
        - Be action-oriented and concise — aim for 3-5 paragraphs
        - Use markdown formatting (headers, bullet points) for readability
        - Only reference information that appears in the provided data
        - Do NOT invent or hallucinate information
        - Prioritize items most likely to come up in the meeting
        """;

    public static string BuildUserPrompt(
        string personName,
        string? personRole,
        string? personTeam,
        IEnumerable<MentionContextForPrompt> mentions,
        IEnumerable<CommitmentContextForPrompt> commitments)
    {
        var lines = new List<string>
        {
            $"## Person: {personName}",
        };

        if (!string.IsNullOrWhiteSpace(personRole))
            lines.Add($"**Role:** {personRole}");
        if (!string.IsNullOrWhiteSpace(personTeam))
            lines.Add($"**Team:** {personTeam}");

        lines.Add("");
        lines.Add("## Recent Transcript Mentions");

        var mentionList = mentions.ToList();
        if (mentionList.Count == 0)
        {
            lines.Add("No recent mentions found.");
        }
        else
        {
            foreach (var m in mentionList)
            {
                lines.Add($"### {m.CaptureTitle ?? "Untitled"} ({m.CapturedAt:yyyy-MM-dd})");
                if (!string.IsNullOrWhiteSpace(m.ExtractionSummary))
                    lines.Add($"**Summary:** {m.ExtractionSummary}");
                if (!string.IsNullOrWhiteSpace(m.MentionContext))
                    lines.Add($"**Context:** {m.MentionContext}");
                lines.Add("");
            }
        }

        lines.Add("## Open Commitments");
        var commitmentList = commitments.ToList();
        if (commitmentList.Count == 0)
        {
            lines.Add("No open commitments.");
        }
        else
        {
            foreach (var c in commitmentList)
            {
                var direction = c.Direction == "MineToThem" ? "I owe them" : "They owe me";
                var due = c.DueDate is not null ? $" (due {c.DueDate})" : "";
                var overdue = c.IsOverdue ? " [OVERDUE]" : "";
                lines.Add($"- [{direction}] {c.Description}{due}{overdue}");
            }
        }

        return string.Join("\n", lines);
    }
}

public sealed record MentionContextForPrompt(
    string? CaptureTitle,
    DateTimeOffset CapturedAt,
    string? ExtractionSummary,
    string? MentionContext);

public sealed record CommitmentContextForPrompt(
    string Description,
    string Direction,
    DateOnly? DueDate,
    bool IsOverdue);
