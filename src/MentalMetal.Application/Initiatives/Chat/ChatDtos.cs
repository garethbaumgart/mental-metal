using MentalMetal.Domain.ChatThreads;

namespace MentalMetal.Application.Initiatives.Chat;

public sealed record SourceReferenceDto(
    string EntityType,
    Guid EntityId,
    string? SnippetText,
    decimal? RelevanceScore)
{
    public static SourceReferenceDto From(SourceReference r) =>
        new(r.EntityType.ToString(), r.EntityId, r.SnippetText, r.RelevanceScore);
}

public sealed record TokenUsageDto(int PromptTokens, int CompletionTokens)
{
    public static TokenUsageDto? From(TokenUsage? t) => t is null ? null : new(t.PromptTokens, t.CompletionTokens);
}

public sealed record ChatMessageDto(
    int MessageOrdinal,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    IReadOnlyList<SourceReferenceDto> SourceReferences,
    TokenUsageDto? TokenUsage)
{
    public static ChatMessageDto From(ChatMessage m) => new(
        m.MessageOrdinal,
        m.Role.ToString(),
        m.Content,
        m.CreatedAt,
        m.SourceReferences.Select(SourceReferenceDto.From).ToList(),
        TokenUsageDto.From(m.TokenUsage));
}

public sealed record ChatThreadDto(
    Guid Id,
    Guid UserId,
    string ContextScopeType,
    Guid? ContextInitiativeId,
    string Title,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastMessageAt,
    int MessageCount,
    IReadOnlyList<ChatMessageDto> Messages)
{
    public static ChatThreadDto From(ChatThread t, bool includeMessages) => new(
        t.Id,
        t.UserId,
        t.Scope.Type.ToString(),
        t.Scope.InitiativeId,
        t.Title,
        t.Status.ToString(),
        t.CreatedAt,
        t.LastMessageAt,
        t.MessageCount,
        includeMessages
            ? t.Messages.Select(ChatMessageDto.From).ToList()
            : []);
}

public sealed record ChatThreadSummaryDto(
    Guid Id,
    string Title,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastMessageAt,
    int MessageCount)
{
    public static ChatThreadSummaryDto From(ChatThread t) => new(
        t.Id, t.Title, t.Status.ToString(), t.CreatedAt, t.LastMessageAt, t.MessageCount);
}

public sealed record RenameChatThreadRequest(string Title);
public sealed record PostChatMessageRequest(string Content);

public sealed record PostChatMessageResponse(
    ChatMessageDto UserMessage,
    ChatMessageDto AssistantMessage,
    ChatThreadSummaryDto Thread);
