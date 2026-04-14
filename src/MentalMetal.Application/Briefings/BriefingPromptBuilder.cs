using System.Text.Json;
using MentalMetal.Application.Common.Ai;

namespace MentalMetal.Application.Briefings;

/// <summary>
/// Produces the prompts handed to <see cref="IAiCompletionService"/>.
/// Facts JSON is the only source of truth - the system prompt forbids invention.
/// </summary>
public sealed class BriefingPromptBuilder
{
    // Lower-camel-case JSON for easier reading by humans + matches frontend conventions.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private const string SystemPrompt =
        "You are an engineering manager's daily briefing assistant. " +
        "Use ONLY the facts in the user message. " +
        "Do not invent commitments, people, dates, counts, or initiatives that are not in the facts. " +
        "Output concise, scannable Markdown. " +
        "Do not include any preamble like 'Here is your briefing'. " +
        "Use short sentences and bulleted lists where appropriate.";

    public AiCompletionRequest BuildMorning(object facts, int maxTokens)
    {
        var json = Serialize(facts);
        var userPrompt =
            "Generate the user's morning briefing as Markdown.\n\n" +
            "Required structure:\n" +
            "1. A single-sentence \"Focus today\" line.\n" +
            "2. ## Today\u2019s priorities — bullet list referencing items from `topCommitmentsDueToday`.\n" +
            "3. ## Today\u2019s 1:1s — bullet list from `oneOnOnesToday` (omit section if empty).\n" +
            "4. ## Overdue delegations — bullet list from `overdueDelegations` (omit if empty).\n" +
            "5. ## People to check in with — bullet list from `peopleNeedingAttention` (omit if empty).\n\n" +
            "Facts (JSON):\n```json\n" + json + "\n```";
        return new AiCompletionRequest(SystemPrompt, userPrompt, MaxTokens: maxTokens, Temperature: 0.3f);
    }

    public AiCompletionRequest BuildWeekly(object facts, int maxTokens)
    {
        var json = Serialize(facts);
        var userPrompt =
            "Generate the user's weekly briefing as Markdown.\n\n" +
            "Required structure:\n" +
            "1. A single-sentence \"Focus this week\" line.\n" +
            "2. ## Milestones this week — bullet list from `milestonesThisWeek` (omit section if empty).\n" +
            "3. ## Overdue — combined bullet list from `overdueCommitments` + `overdueDelegations` (omit if both empty).\n" +
            "4. ## Initiatives needing attention — from `initiativesNeedingAttention` (omit if empty).\n" +
            "5. ## People needing 1:1 time — from `peopleWithoutRecentOneOnOne` (omit if empty).\n\n" +
            "Facts (JSON):\n```json\n" + json + "\n```";
        return new AiCompletionRequest(SystemPrompt, userPrompt, MaxTokens: maxTokens, Temperature: 0.3f);
    }

    public AiCompletionRequest BuildOneOnOnePrep(object facts, int maxTokens)
    {
        var json = Serialize(facts);
        var userPrompt =
            "Generate a 1:1 prep sheet as Markdown.\n\n" +
            "Required structure:\n" +
            "1. ## Context — 2-3 sentences synthesising `lastOneOnOne`, `openGoals`, and `recentObservations`.\n" +
            "2. ## Open items — bullets referencing `openCommitmentsWithPerson` and `openDelegationsToPerson`.\n" +
            "3. ## Recent observations — bullets from `recentObservations`.\n" +
            "4. ## Suggested talking points — EXACTLY 3 to 5 bulleted talking points grounded in the facts.\n\n" +
            "Facts (JSON):\n```json\n" + json + "\n```";
        return new AiCompletionRequest(SystemPrompt, userPrompt, MaxTokens: maxTokens, Temperature: 0.3f);
    }

    public string SerializeFacts(object facts) => Serialize(facts);

    private static string Serialize(object facts) => JsonSerializer.Serialize(facts, JsonOptions);
}
