using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.ChatThreads;

public sealed class ChatThread : AggregateRoot, IUserScoped
{
    public const int MaxTitleLength = 200;
    public const int AutoTitleMaxLength = 80;

    public const string RenameSourceAutoFromFirstMessage = "AutoFromFirstMessage";
    public const string RenameSourceManual = "Manual";

    private readonly List<ChatMessage> _messages = [];

    public Guid UserId { get; private set; }
    public ContextScope Scope { get; private set; } = null!;
    public string Title { get; private set; } = string.Empty;
    public ChatThreadStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastMessageAt { get; private set; }
    public int MessageCount { get; private set; }
    public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();

    private ChatThread() { }

    public static ChatThread Start(Guid userId, ContextScope scope)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        ArgumentNullException.ThrowIfNull(scope);

        var now = DateTimeOffset.UtcNow;
        var thread = new ChatThread
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Scope = scope,
            Title = string.Empty,
            Status = ChatThreadStatus.Active,
            CreatedAt = now,
            LastMessageAt = null,
            MessageCount = 0,
        };

        thread.RaiseDomainEvent(new ChatThreadStarted(thread.Id, userId, scope.Type, scope.InitiativeId));
        return thread;
    }

    public ChatMessage AppendUserMessage(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));
        EnsureActive();

        var now = DateTimeOffset.UtcNow;
        var ordinal = _messages.Count + 1;
        var msg = ChatMessage.Create(ordinal, ChatRole.User, content.Trim(), now);
        _messages.Add(msg);
        LastMessageAt = now;
        MessageCount = _messages.Count;

        // Auto-title from first user message.
        if (ordinal == 1 && string.IsNullOrWhiteSpace(Title))
        {
            var derived = DeriveAutoTitle(content);
            Title = derived;
            RaiseDomainEvent(new ChatThreadRenamed(Id, UserId, Title, RenameSourceAutoFromFirstMessage));
        }

        RaiseDomainEvent(new ChatMessageSent(Id, UserId, ordinal));
        return msg;
    }

    public ChatMessage AppendAssistantMessage(
        string content,
        IReadOnlyList<SourceReference>? sourceReferences = null,
        TokenUsage? tokenUsage = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));
        EnsureActive();

        var now = DateTimeOffset.UtcNow;
        var ordinal = _messages.Count + 1;
        var msg = ChatMessage.Create(ordinal, ChatRole.Assistant, content, now, sourceReferences, tokenUsage);
        _messages.Add(msg);
        LastMessageAt = now;
        MessageCount = _messages.Count;

        RaiseDomainEvent(new ChatMessageReceived(Id, UserId, ordinal));
        return msg;
    }

    public ChatMessage AppendSystemMessage(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));
        // System messages can be appended to archived threads too? Design: error/limit notices fit
        // into the conversation as it stands; still require Active to avoid surprise writes on archived rows.
        EnsureActive();

        var now = DateTimeOffset.UtcNow;
        var ordinal = _messages.Count + 1;
        var msg = ChatMessage.Create(ordinal, ChatRole.System, content, now);
        _messages.Add(msg);
        LastMessageAt = now;
        MessageCount = _messages.Count;
        return msg;
    }

    public void Rename(string title, string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        var trimmed = title.Trim();
        if (trimmed.Length > MaxTitleLength)
            throw new ArgumentException($"Title must be <= {MaxTitleLength} characters.", nameof(title));

        Title = trimmed;
        RaiseDomainEvent(new ChatThreadRenamed(Id, UserId, Title, source ?? RenameSourceManual));
    }

    public void Archive()
    {
        if (Status == ChatThreadStatus.Archived)
            throw new InvalidOperationException("Thread is already archived.");

        Status = ChatThreadStatus.Archived;
        RaiseDomainEvent(new ChatThreadArchived(Id, UserId));
    }

    public void Unarchive()
    {
        if (Status == ChatThreadStatus.Active)
            throw new InvalidOperationException("Thread is already active.");

        Status = ChatThreadStatus.Active;
        RaiseDomainEvent(new ChatThreadUnarchived(Id, UserId));
    }

    private void EnsureActive()
    {
        if (Status != ChatThreadStatus.Active)
            throw new InvalidOperationException("Cannot modify an archived chat thread.");
    }

    private static string DeriveAutoTitle(string firstUserMessage)
    {
        var source = firstUserMessage.Trim();
        if (source.Length <= AutoTitleMaxLength)
            return source;
        // Reserve one character for the ellipsis so the final length equals AutoTitleMaxLength.
        return source[..(AutoTitleMaxLength - 1)] + "…";
    }
}
