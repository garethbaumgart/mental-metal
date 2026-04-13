using System.Text;
using MentalMetal.Application.Chat.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.ChatThreads;

namespace MentalMetal.Application.Chat.Global;

public sealed class GlobalChatCompletionService(
    IIntentClassifier intentClassifier,
    IGlobalChatContextBuilder contextBuilder,
    IAiCompletionService ai) : IGlobalChatCompletionService
{
    public const int HistoryTokenBudget = 4000;
    public const int MaxHistoryMessages = 20;
    public const int CompletionMaxTokens = 1200;

    public async Task GenerateReplyAsync(Guid userId, ChatThread thread, CancellationToken cancellationToken)
    {
        if (thread.Scope.Type != ContextScopeType.Global)
            throw new InvalidOperationException("GenerateReplyAsync only supports global-scoped threads.");

        var lastMessage = thread.Messages[^1];
        if (lastMessage.Role != ChatRole.User)
            throw new InvalidOperationException("Last thread message must be a User message before generating a reply.");

        var history = TrimHistory(thread.Messages);

        var intents = await intentClassifier.ClassifyAsync(userId, lastMessage.Content, cancellationToken);
        var context = await contextBuilder.BuildAsync(userId, intents, history, cancellationToken);

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

    private static string BuildSystemPrompt(GlobalChatContextPayload ctx)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are the user's work assistant. Answer using ONLY the supplied context.");
        sb.AppendLine("Cite every factual claim using an EntityId drawn from the context. Do not invent EntityIds.");
        sb.AppendLine("If the question cannot be answered from the supplied context, say so politely and suggest what the user could capture or add.");
        sb.AppendLine("Refuse out-of-scope questions (politics, general world knowledge) by redirecting to the user's own work data.");
        sb.AppendLine();
        sb.AppendLine("Respond with a single JSON object with this exact shape:");
        sb.AppendLine("{\"assistantText\": \"<your reply>\", \"sourceReferences\": [{\"entityType\": \"<Type>\", \"entityId\": \"<guid>\", \"snippetText\": \"<short quote>\", \"relevanceScore\": <0..1 or null>}]}");
        sb.AppendLine("Valid entityType values: Person, Initiative, Capture, Commitment, Delegation, LivingBriefDecision, LivingBriefRisk, LivingBriefRequirements, LivingBriefDesignDirection.");
        sb.AppendLine();
        sb.AppendLine($"### Counters");
        sb.AppendLine($"- Open commitments: {ctx.Counters.OpenCommitments}");
        sb.AppendLine($"- Open delegations: {ctx.Counters.OpenDelegations}");
        sb.AppendLine($"- Active initiatives: {ctx.Counters.ActiveInitiatives}");

        if (ctx.Persons.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Persons");
            foreach (var p in ctx.Persons)
                sb.AppendLine($"- [Person {p.Id}] {p.Name} ({p.Type}{(string.IsNullOrWhiteSpace(p.Role) ? "" : ", " + p.Role)}{(string.IsNullOrWhiteSpace(p.Team) ? "" : ", " + p.Team)})");
        }

        if (ctx.Initiatives.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Initiatives");
            foreach (var i in ctx.Initiatives)
            {
                sb.AppendLine($"- [Initiative {i.Id}] {i.Title} ({i.Status})");
                if (!string.IsNullOrWhiteSpace(i.BriefSummary))
                    sb.AppendLine($"  Summary: {i.BriefSummary}");
            }
        }

        if (ctx.Commitments.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Commitments");
            foreach (var c in ctx.Commitments)
                sb.AppendLine($"- [Commitment {c.Id}] ({c.Status}, {c.Direction}, {c.PersonName ?? "unknown"}) {c.Description}{(c.DueDate is null ? "" : $" — due {c.DueDate}")}{(c.IsOverdue ? " [OVERDUE]" : "")}");
        }

        if (ctx.Delegations.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Delegations");
            foreach (var d in ctx.Delegations)
                sb.AppendLine($"- [Delegation {d.Id}] ({d.Status}, to {d.DelegateName ?? "unknown"}) {d.Description}{(d.DueDate is null ? "" : $" — due {d.DueDate}")}{(d.IsOverdue ? " [OVERDUE]" : "")}{(string.IsNullOrWhiteSpace(d.BlockedReason) ? "" : $" [blocked: {d.BlockedReason}]")}");
        }

        if (ctx.Captures.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Capture summaries");
            foreach (var c in ctx.Captures)
                sb.AppendLine($"- [Capture {c.Id}] ({c.CreatedAt:yyyy-MM-dd}) {c.Summary}");
        }

        if (ctx.TruncationNotes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Notes");
            foreach (var n in ctx.TruncationNotes)
                sb.AppendLine($"- {n}");
        }

        return sb.ToString();
    }

    private static string BuildUserPrompt(IReadOnlyList<ChatMessage> history, string latestUserQuestion)
    {
        var sb = new StringBuilder();
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
        IReadOnlyList<ChatMessage> capped = messages.Count <= MaxHistoryMessages
            ? messages
            : messages.Skip(messages.Count - MaxHistoryMessages).ToList();

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
