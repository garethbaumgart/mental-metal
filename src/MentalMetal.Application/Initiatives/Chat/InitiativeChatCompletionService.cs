using System.Text;
using MentalMetal.Application.Chat.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.ChatThreads;

namespace MentalMetal.Application.Initiatives.Chat;

public sealed class InitiativeChatCompletionService(
    IInitiativeChatContextBuilder contextBuilder,
    IAiCompletionService ai) : IInitiativeChatCompletionService
{
    // Conservative cap so we never blow the context window for the history portion. Actual
    // token count is estimated as chars/4 which is close enough for the default providers.
    public const int HistoryTokenBudget = 4000;
    public const int MaxHistoryMessages = 20;
    public const int CompletionMaxTokens = 1200;

    public async Task GenerateReplyAsync(
        Guid userId,
        ChatThread thread,
        CancellationToken cancellationToken)
    {
        if (thread.Scope.Type != ContextScopeType.Initiative || thread.Scope.InitiativeId is null)
            throw new InvalidOperationException("GenerateReplyAsync only supports initiative-scoped threads.");

        // The caller is expected to have already appended the user message; the last message
        // must therefore be Role User.
        var lastMessage = thread.Messages[^1];
        if (lastMessage.Role != ChatRole.User)
            throw new InvalidOperationException("Last thread message must be a User message before generating a reply.");

        var history = TrimHistory(thread.Messages);
        var context = await contextBuilder.BuildAsync(userId, thread.Scope.InitiativeId.Value, lastMessage.Content, history, cancellationToken);
        if (context is null)
        {
            // Should never happen for authorised callers — but emit a friendly fallback rather than throwing.
            thread.AppendSystemMessage("Unable to assemble context for this initiative.");
            return;
        }

        var systemPrompt = BuildSystemPrompt(context);
        var userPrompt = BuildUserPrompt(history, lastMessage.Content);

        try
        {
            var result = await ai.CompleteAsync(
                new AiCompletionRequest(systemPrompt, userPrompt, MaxTokens: CompletionMaxTokens, Temperature: 0.2f),
                cancellationToken);

            var envelope = ChatResponseParser.TryParse(result.Content);
            var known = context.KnownCitations();

            if (envelope is null)
            {
                // Fallback: raw text, no citations.
                thread.AppendAssistantMessage(
                    result.Content,
                    sourceReferences: [],
                    tokenUsage: new TokenUsage(result.InputTokens, result.OutputTokens));
                return;
            }

            var refs = envelope.SourceReferences
                .Select(r => ChatResponseParser.TryBuildReference(r, known))
                .Where(r => r is not null)
                .Select(r => r!)
                .ToList();

            thread.AppendAssistantMessage(
                string.IsNullOrWhiteSpace(envelope.AssistantText) ? result.Content : envelope.AssistantText,
                refs,
                new TokenUsage(result.InputTokens, result.OutputTokens));
        }
        catch (TasteLimitExceededException)
        {
            thread.AppendSystemMessage("Daily AI limit reached");
        }
        catch (AiProviderException)
        {
            thread.AppendAssistantMessage(
                "AI service unavailable, please retry.",
                sourceReferences: [],
                tokenUsage: null);
        }
    }

    private static string BuildSystemPrompt(InitiativeChatContextPayload context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an assistant answering questions about a single initiative.");
        sb.AppendLine("Use ONLY the supplied context. If a question cannot be answered from the context, say so politely and refuse out-of-scope questions.");
        sb.AppendLine("Cite every factual claim using an EntityId drawn from the context. Do not invent EntityIds.");
        sb.AppendLine();
        sb.AppendLine("Respond with a single JSON object with this exact shape:");
        sb.AppendLine("{\"assistantText\": \"<your reply>\", \"sourceReferences\": [{\"entityType\": \"<Type>\", \"entityId\": \"<guid>\", \"snippetText\": \"<short quote>\", \"relevanceScore\": <0..1 or null>}]}");
        sb.AppendLine("Valid entityType values: Capture, Commitment, Delegation, LivingBriefDecision, LivingBriefRisk, LivingBriefRequirements, LivingBriefDesignDirection, Initiative.");
        sb.AppendLine();
        sb.AppendLine("### Initiative");
        sb.AppendLine($"- Id: {context.Initiative.Id}");
        sb.AppendLine($"- Title: {context.Initiative.Title}");
        sb.AppendLine($"- Status: {context.Initiative.Status}");
        if (context.Initiative.Milestones.Count > 0)
        {
            sb.AppendLine("- Milestones:");
            foreach (var m in context.Initiative.Milestones)
                sb.AppendLine($"  - {m.Title} (target {m.TargetDate}; completed={m.IsCompleted})");
        }

        sb.AppendLine();
        sb.AppendLine("### Living Brief");
        if (!string.IsNullOrWhiteSpace(context.LivingBrief.Summary))
            sb.AppendLine($"Summary: {context.LivingBrief.Summary}");
        if (context.LivingBrief.RecentDecisions.Count > 0)
        {
            sb.AppendLine("Recent decisions:");
            foreach (var d in context.LivingBrief.RecentDecisions)
                sb.AppendLine($"- [LivingBriefDecision {d.Id}] {d.Description}{(string.IsNullOrWhiteSpace(d.Rationale) ? "" : $" — {d.Rationale}")}");
        }
        if (context.LivingBrief.OpenRisks.Count > 0)
        {
            sb.AppendLine("Open risks:");
            foreach (var r in context.LivingBrief.OpenRisks)
                sb.AppendLine($"- [LivingBriefRisk {r.Id}] ({r.Severity}) {r.Description}");
        }
        if (context.LivingBrief.LatestRequirementsId is { } reqId)
            sb.AppendLine($"Latest requirements [LivingBriefRequirements {reqId}]: {context.LivingBrief.LatestRequirementsContent}");
        if (context.LivingBrief.LatestDesignDirectionId is { } ddId)
            sb.AppendLine($"Latest design direction [LivingBriefDesignDirection {ddId}]: {context.LivingBrief.LatestDesignDirectionContent}");

        if (context.Commitments.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Commitments");
            foreach (var c in context.Commitments)
                sb.AppendLine($"- [Commitment {c.Id}] ({c.Status}, {c.Direction}, {c.PersonName ?? "unknown"}) {c.Description}{(c.DueDate is null ? "" : $" — due {c.DueDate}")}");
        }

        if (context.Delegations.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Delegations");
            foreach (var d in context.Delegations)
                sb.AppendLine($"- [Delegation {d.Id}] ({d.Status}, to {d.DelegateName ?? "unknown"}) {d.Description}{(d.DueDate is null ? "" : $" — due {d.DueDate}")}{(string.IsNullOrWhiteSpace(d.BlockedReason) ? "" : $" [blocked: {d.BlockedReason}]")}");
        }

        if (context.LinkedCaptures.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Capture summaries");
            foreach (var c in context.LinkedCaptures)
                sb.AppendLine($"- [Capture {c.Id}] ({c.CreatedAt:yyyy-MM-dd}) {c.Summary}");
        }

        return sb.ToString();
    }

    private static string BuildUserPrompt(IReadOnlyList<ChatMessage> history, string latestUserQuestion)
    {
        var sb = new StringBuilder();
        // The final User message is the latest question and is already part of `history`, but we
        // surface it last explicitly so the model focuses on the current turn.
        foreach (var m in history.Take(history.Count - 1))
        {
            sb.AppendLine($"### {m.Role}");
            sb.AppendLine(m.Content);
            sb.AppendLine();
        }
        sb.AppendLine("### Current question");
        sb.AppendLine(latestUserQuestion);
        return sb.ToString();
    }

    internal static IReadOnlyList<ChatMessage> TrimHistory(IReadOnlyList<ChatMessage> messages)
    {
        // Step 1: enforce the hard message-count cap. Keep the most recent N; oldest trimmed first.
        IReadOnlyList<ChatMessage> capped = messages.Count <= MaxHistoryMessages
            ? messages
            : messages.Skip(messages.Count - MaxHistoryMessages).ToList();

        // Step 2: enforce the token budget. Estimate tokens as chars/4 (good enough for the
        // default providers). Walk from newest to oldest, keeping as many messages as fit; always
        // keep at least the most recent message so we have a question to answer.
        var total = 0;
        var keepFromIndex = capped.Count;
        for (var i = capped.Count - 1; i >= 0; i--)
        {
            var estimate = EstimateTokens(capped[i].Content);
            if (i < capped.Count - 1 && total + estimate > HistoryTokenBudget) break;
            total += estimate;
            keepFromIndex = i;
        }

        return keepFromIndex == 0 ? capped : capped.Skip(keepFromIndex).ToList();
    }

    private static int EstimateTokens(string content) =>
        string.IsNullOrEmpty(content) ? 0 : (content.Length + 3) / 4;
}
