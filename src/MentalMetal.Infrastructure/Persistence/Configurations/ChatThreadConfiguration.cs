using System.Text.Json;
using MentalMetal.Domain.ChatThreads;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class ChatThreadConfiguration : IEntityTypeConfiguration<ChatThread>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = false,
    };

    public void Configure(EntityTypeBuilder<ChatThread> builder)
    {
        builder.ToTable("ChatThreads");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.Title).HasMaxLength(ChatThread.MaxTitleLength).IsRequired().HasDefaultValue(string.Empty);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.LastMessageAt);
        builder.Property(t => t.MessageCount).HasDefaultValue(0);

        builder.OwnsOne(t => t.Scope, scope =>
        {
            scope.Property(s => s.Type)
                .HasColumnName("ContextScopeType")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            scope.Property(s => s.InitiativeId)
                .HasColumnName("ContextInitiativeId");
        });

        builder.Navigation(t => t.Scope).IsRequired();

        // Public Messages is a read-only projection of the backing list; the JSON column
        // below is the source of truth.
        builder.Ignore(t => t.Messages);

        // Embedded messages serialised as JSONB. ChatMessage is an internal-immutable VO; we
        // serialise via shim records to avoid leaking EF plumbing into the domain API.
        ConfigureMessagesJsonBackingField(builder);

        builder.HasIndex(t => new { t.UserId, t.Status });
    }

    private static void ConfigureMessagesJsonBackingField(EntityTypeBuilder<ChatThread> builder)
    {
        var converter = new ValueConverter<List<ChatMessage>, string>(
            list => SerializeMessages(list),
            json => DeserializeMessages(json));

        var comparer = new ValueComparer<List<ChatMessage>>(
            (a, b) => SerializeMessages(a) == SerializeMessages(b),
            v => SerializeMessages(v).GetHashCode(),
            v => DeserializeMessages(SerializeMessages(v)));

        builder.Property<List<ChatMessage>>("_messages")
            .HasField("_messages")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("Messages")
            .HasColumnType("jsonb")
            .HasConversion(converter, comparer)
            .HasDefaultValueSql("'[]'::jsonb");
    }

    private static string SerializeMessages(List<ChatMessage>? messages)
    {
        var dtos = (messages ?? [])
            .Select(m => new PersistedChatMessage(
                m.MessageOrdinal,
                m.Role.ToString(),
                m.Content,
                m.CreatedAt,
                m.SourceReferences
                    .Select(r => new PersistedSourceReference(
                        r.EntityType.ToString(),
                        r.EntityId,
                        r.SnippetText,
                        r.RelevanceScore))
                    .ToList(),
                m.TokenUsage is null ? null : new PersistedTokenUsage(m.TokenUsage.PromptTokens, m.TokenUsage.CompletionTokens)))
            .ToList();
        return JsonSerializer.Serialize(dtos, JsonOpts);
    }

    private static List<ChatMessage> DeserializeMessages(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return [];

        var list = JsonSerializer.Deserialize<List<PersistedChatMessage>>(json, JsonOpts) ?? [];
        return list
            .Select(RebuildMessage)
            .ToList();
    }

    private static ChatMessage RebuildMessage(PersistedChatMessage p)
    {
        var role = Enum.Parse<ChatRole>(p.Role, ignoreCase: true);
        var refs = (p.SourceReferences ?? [])
            .Select(r => new SourceReference(
                Enum.Parse<SourceReferenceEntityType>(r.EntityType, ignoreCase: true),
                r.EntityId,
                r.SnippetText,
                r.RelevanceScore))
            .ToList();
        var tokens = p.TokenUsage is null
            ? null
            : new TokenUsage(p.TokenUsage.PromptTokens, p.TokenUsage.CompletionTokens);

        // Invoke internal factory via reflection — the rehydration path must not re-run the
        // SourceReferences-on-Assistant-only invariant because the data has already been
        // validated when it was first written.
        return UnsafeCreate(p.MessageOrdinal, role, p.Content, p.CreatedAt, refs, tokens);
    }

    private static ChatMessage UnsafeCreate(
        int ordinal,
        ChatRole role,
        string content,
        DateTimeOffset createdAt,
        List<SourceReference> refs,
        TokenUsage? tokens)
    {
        // Assistant path accepts references directly.
        if (role == ChatRole.Assistant || refs.Count == 0)
        {
            return InvokeInternalCreate(ordinal, role, content, createdAt, refs, tokens);
        }
        // Defensive: drop references on non-assistant rows (should never happen with valid data).
        return InvokeInternalCreate(ordinal, role, content, createdAt, [], tokens);
    }

    private static ChatMessage InvokeInternalCreate(
        int ordinal,
        ChatRole role,
        string content,
        DateTimeOffset createdAt,
        IReadOnlyList<SourceReference> refs,
        TokenUsage? tokens)
    {
        var method = typeof(ChatMessage).GetMethod(
            "Create",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (ChatMessage)method.Invoke(null, [ordinal, role, content, createdAt, refs, tokens])!;
    }

    private sealed record PersistedChatMessage(
        int MessageOrdinal,
        string Role,
        string Content,
        DateTimeOffset CreatedAt,
        List<PersistedSourceReference>? SourceReferences,
        PersistedTokenUsage? TokenUsage);

    private sealed record PersistedSourceReference(
        string EntityType,
        Guid EntityId,
        string? SnippetText,
        decimal? RelevanceScore);

    private sealed record PersistedTokenUsage(int PromptTokens, int CompletionTokens);
}
