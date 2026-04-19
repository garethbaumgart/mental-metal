namespace MentalMetal.Application.Captures.AutoExtract;

/// <summary>
/// Builds the system and user prompts for AI-based capture extraction.
/// </summary>
public static class ExtractionPromptBuilder
{
    public static string BuildSystemPrompt(string userName) => $$"""
        You are an AI assistant that extracts structured information from meeting transcripts and notes.
        You MUST return ONLY valid JSON — no markdown, no commentary, no code fences.

        These notes were written or recorded by {{userName}}. All commitment directions are
        from {{userName}}'s perspective.

        Extract the following from the provided text:

        1. **people_mentioned**: People referenced by name. For each person provide:
           - "raw_name": The name as it appears in the text
           - "context": A brief phrase describing their role or mention context (or null)
           - Do NOT include {{userName}} in this list — they are the note-taker, not a mentioned person.

        2. **commitments**: Action items, promises, or obligations. For each:
           - "description": Clear description of the commitment
           - "direction": "MineToThem" if {{userName}} committed to do something for someone else, or "TheirsToMe" if someone else committed to do something for {{userName}}
           - "person_raw_name": The name of the OTHER party (not {{userName}}). If {{userName}} is the one who committed, person_raw_name is who they committed TO. If someone else committed, person_raw_name is who committed.
           - "due_date": ISO 8601 date if a deadline was mentioned (or null)
           - "confidence": One of "High", "Medium", or "Low"
             - High = explicit verbal promise with a specific person and timeframe (e.g. "Alice will send the report by Friday")
             - Medium = clear intent but missing person or timeframe (e.g. "We need to update the docs")
             - Low = ambiguous or speculative (e.g. "Maybe we should look into that")

        3. **decisions**: Concrete decisions made during the discussion. Short sentences.

        4. **risks**: Risks, concerns, or blockers mentioned. Short sentences.

        5. **initiative_tags**: Project or initiative names referenced. For each:
           - "raw_name": The project/initiative name as mentioned
           - "context": Brief context of the mention (or null)

        6. **summary**: A 2-3 sentence summary of the key discussion points and outcomes.

        Rules:
        - Only extract what is EXPLICITLY stated in the text. Do NOT infer or hallucinate.
        - If the text is a quick note with no clear commitments, return empty arrays.
        - For speaker-labelled transcripts, each line may start with a speaker name followed by a colon.
        - Return valid JSON matching this exact schema (no extra fields):

        {
          "summary": "string",
          "people_mentioned": [{"raw_name": "string", "context": "string|null"}],
          "commitments": [{"description": "string", "direction": "MineToThem|TheirsToMe", "person_raw_name": "string|null", "due_date": "string|null", "confidence": "High|Medium|Low"}],
          "decisions": ["string"],
          "risks": ["string"],
          "initiative_tags": [{"raw_name": "string", "context": "string|null"}]
        }
        """;

    public static string BuildUserPrompt(string captureContent) =>
        $"Extract structured information from the following text:\n\n{captureContent}";
}
