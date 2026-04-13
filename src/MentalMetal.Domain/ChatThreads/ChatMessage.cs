namespace MentalMetal.Domain.ChatThreads;

/// <summary>
/// Value object embedded on <see cref="ChatThread"/>. Persisted as part of the thread's JSONB payload.
/// </summary>
public sealed class ChatMessage
{
    public int MessageOrdinal { get; private set; }
    public ChatRole Role { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<SourceReference> _sourceReferences = [];
    public IReadOnlyList<SourceReference> SourceReferences => _sourceReferences.AsReadOnly();

    public TokenUsage? TokenUsage { get; private set; }

    // EF Core / JSON deserialisation ctor.
    private ChatMessage() { }

    internal static ChatMessage Create(
        int messageOrdinal,
        ChatRole role,
        string content,
        DateTimeOffset createdAt,
        IReadOnlyList<SourceReference>? sourceReferences = null,
        TokenUsage? tokenUsage = null)
    {
        if (messageOrdinal < 1)
            throw new ArgumentOutOfRangeException(nameof(messageOrdinal), "MessageOrdinal must be >= 1.");

        content ??= string.Empty;

        var refs = sourceReferences is null ? [] : sourceReferences.ToList();
        if (refs.Count > 0 && role != ChatRole.Assistant)
            throw new ArgumentException("SourceReferences are only valid on Assistant messages.", nameof(sourceReferences));

        return new ChatMessage
        {
            MessageOrdinal = messageOrdinal,
            Role = role,
            Content = content,
            CreatedAt = createdAt,
            TokenUsage = tokenUsage,
            _sourceReferences = { },
        }.WithReferences(refs);
    }

    private ChatMessage WithReferences(IEnumerable<SourceReference> refs)
    {
        _sourceReferences.AddRange(refs);
        return this;
    }
}
