using MentalMetal.Domain.ChatThreads;

namespace MentalMetal.Domain.Tests.ChatThreads;

public class ChatThreadTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid InitiativeId = Guid.NewGuid();

    private static ChatThread NewThread() =>
        ChatThread.Start(UserId, ContextScope.Initiative(InitiativeId));

    [Fact]
    public void Start_SetsInvariants_AndRaisesEvent()
    {
        var thread = NewThread();

        Assert.Equal(UserId, thread.UserId);
        Assert.Equal(ContextScopeType.Initiative, thread.Scope.Type);
        Assert.Equal(InitiativeId, thread.Scope.InitiativeId);
        Assert.Equal(ChatThreadStatus.Active, thread.Status);
        Assert.Equal(0, thread.MessageCount);
        Assert.Empty(thread.Messages);
        Assert.Null(thread.LastMessageAt);
        Assert.Equal(string.Empty, thread.Title);

        var started = Assert.IsType<ChatThreadStarted>(Assert.Single(thread.DomainEvents));
        Assert.Equal(thread.Id, started.ThreadId);
        Assert.Equal(UserId, started.UserId);
    }

    [Fact]
    public void Start_WithEmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ChatThread.Start(Guid.Empty, ContextScope.Initiative(InitiativeId)));
    }

    [Fact]
    public void AppendMessages_OrdinalsAreContiguous_AndCountsUpdate()
    {
        var thread = NewThread();

        var u1 = thread.AppendUserMessage("Hello?");
        var a1 = thread.AppendAssistantMessage("Hi!");
        var u2 = thread.AppendUserMessage("What next?");
        var sys = thread.AppendSystemMessage("Daily AI limit reached");

        Assert.Equal(1, u1.MessageOrdinal);
        Assert.Equal(2, a1.MessageOrdinal);
        Assert.Equal(3, u2.MessageOrdinal);
        Assert.Equal(4, sys.MessageOrdinal);
        Assert.Equal(4, thread.MessageCount);
        Assert.NotNull(thread.LastMessageAt);
    }

    [Fact]
    public void AppendUserMessage_FirstMessageSetsAutoTitle_AndRaisesRenamed()
    {
        var thread = NewThread();
        thread.ClearDomainEvents();

        var question = "What is blocking the API spec delivery?";
        thread.AppendUserMessage(question);

        Assert.Equal(question, thread.Title);
        Assert.Contains(thread.DomainEvents, e => e is ChatThreadRenamed r && r.Source == ChatThread.RenameSourceAutoFromFirstMessage);
        Assert.Contains(thread.DomainEvents, e => e is ChatMessageSent);
    }

    [Fact]
    public void AppendUserMessage_LongFirstMessage_IsTruncatedWithEllipsis()
    {
        var thread = NewThread();
        var longMsg = new string('x', 200);

        thread.AppendUserMessage(longMsg);

        Assert.Equal(ChatThread.AutoTitleMaxLength, thread.Title.Length); // 79 chars + ellipsis
        Assert.EndsWith("…", thread.Title);
    }

    [Fact]
    public void AppendUserMessage_SubsequentMessages_DoNotRenameTitle()
    {
        var thread = NewThread();
        thread.AppendUserMessage("first");
        thread.AppendAssistantMessage("ack");

        thread.AppendUserMessage("second which is longer");

        Assert.Equal("first", thread.Title);
    }

    [Fact]
    public void AppendUserMessage_Empty_Throws()
    {
        var thread = NewThread();
        Assert.Throws<ArgumentException>(() => thread.AppendUserMessage(""));
        Assert.Throws<ArgumentException>(() => thread.AppendUserMessage("   "));
    }

    [Fact]
    public void AppendAssistantMessage_AllowsSourceReferencesAndTokenUsage()
    {
        var thread = NewThread();
        thread.AppendUserMessage("q");

        var refs = new List<SourceReference>
        {
            new(SourceReferenceEntityType.LivingBriefDecision, Guid.NewGuid(), "Adopt Postgres"),
        };
        var tokens = new TokenUsage(100, 50);
        var asst = thread.AppendAssistantMessage("Based on the brief…", refs, tokens);

        Assert.Single(asst.SourceReferences);
        Assert.Equal(tokens, asst.TokenUsage);
        Assert.Contains(thread.DomainEvents, e => e is ChatMessageReceived r && r.MessageOrdinal == 2);
    }

    [Fact]
    public void ChatMessage_Create_RejectsSourceReferencesOnNonAssistantMessages()
    {
        var thread = NewThread();
        thread.AppendUserMessage("q");

        var refs = new List<SourceReference>
        {
            new(SourceReferenceEntityType.Capture, Guid.NewGuid()),
        };

        // User / System cannot carry references: exercised indirectly via ChatMessage.Create invariants.
        var ex = Record.Exception(() => InvokeCreate(1, ChatRole.User, "hi", refs));
        Assert.IsType<ArgumentException>(ex);

        var ex2 = Record.Exception(() => InvokeCreate(1, ChatRole.System, "note", refs));
        Assert.IsType<ArgumentException>(ex2);
    }

    // Reflection helper: ChatMessage.Create is internal.
    private static ChatMessage InvokeCreate(int ordinal, ChatRole role, string content, IReadOnlyList<SourceReference>? refs)
    {
        var method = typeof(ChatMessage).GetMethod(
            "Create",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        try
        {
            return (ChatMessage)method.Invoke(null, [ordinal, role, content, DateTimeOffset.UtcNow, refs, null])!;
        }
        catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException is not null)
        {
            throw tie.InnerException;
        }
    }

    [Fact]
    public void Rename_Valid_SetsTitle_AndRaisesManualEvent()
    {
        var thread = NewThread();
        thread.ClearDomainEvents();

        thread.Rename("Q3 Planning Discussion", ChatThread.RenameSourceManual);

        Assert.Equal("Q3 Planning Discussion", thread.Title);
        var renamed = Assert.IsType<ChatThreadRenamed>(Assert.Single(thread.DomainEvents));
        Assert.Equal(ChatThread.RenameSourceManual, renamed.Source);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_EmptyOrWhitespace_Throws(string title)
    {
        var thread = NewThread();
        Assert.Throws<ArgumentException>(() => thread.Rename(title, ChatThread.RenameSourceManual));
    }

    [Fact]
    public void Rename_Over200Chars_Throws()
    {
        var thread = NewThread();
        Assert.Throws<ArgumentException>(() => thread.Rename(new string('y', 201), ChatThread.RenameSourceManual));
    }

    [Fact]
    public void Archive_ActiveThread_MovesToArchived_AndRaisesEvent()
    {
        var thread = NewThread();
        thread.ClearDomainEvents();

        thread.Archive();

        Assert.Equal(ChatThreadStatus.Archived, thread.Status);
        Assert.IsType<ChatThreadArchived>(Assert.Single(thread.DomainEvents));
    }

    [Fact]
    public void Archive_AlreadyArchived_Throws()
    {
        var thread = NewThread();
        thread.Archive();

        Assert.Throws<InvalidOperationException>(() => thread.Archive());
    }

    [Fact]
    public void Unarchive_ActiveThread_Throws()
    {
        var thread = NewThread();
        Assert.Throws<InvalidOperationException>(() => thread.Unarchive());
    }

    [Fact]
    public void Unarchive_ArchivedThread_MovesToActive()
    {
        var thread = NewThread();
        thread.Archive();
        thread.ClearDomainEvents();

        thread.Unarchive();

        Assert.Equal(ChatThreadStatus.Active, thread.Status);
        Assert.IsType<ChatThreadUnarchived>(Assert.Single(thread.DomainEvents));
    }

    [Fact]
    public void AppendUserMessage_OnArchivedThread_Throws()
    {
        var thread = NewThread();
        thread.Archive();
        Assert.Throws<InvalidOperationException>(() => thread.AppendUserMessage("hi"));
    }

    [Fact]
    public void ContextScope_Initiative_RejectsEmptyGuid()
    {
        Assert.Throws<ArgumentException>(() => ContextScope.Initiative(Guid.Empty));
    }

    [Fact]
    public void ContextScope_Global_IsReserved()
    {
        var scope = ContextScope.Global();
        Assert.Equal(ContextScopeType.Global, scope.Type);
        Assert.Null(scope.InitiativeId);
    }

    [Fact]
    public void ContextScope_Global_EqualityAndDistinctFromInitiative()
    {
        var a = ContextScope.Global();
        var b = ContextScope.Global();
        var i = ContextScope.Initiative(InitiativeId);

        Assert.Equal(a, b);
        Assert.NotEqual(a, i);
    }

    [Fact]
    public void Start_Global_RaisesBothChatThreadStartedAndGlobalChatThreadStarted()
    {
        var thread = ChatThread.Start(UserId, ContextScope.Global());

        Assert.Contains(thread.DomainEvents, e => e is ChatThreadStarted s && s.ScopeType == ContextScopeType.Global && s.InitiativeId == null);
        Assert.Contains(thread.DomainEvents, e => e is GlobalChatThreadStarted g && g.ThreadId == thread.Id && g.UserId == UserId);
    }

    [Fact]
    public void Start_Initiative_DoesNotRaiseGlobalChatThreadStarted()
    {
        var thread = NewThread();
        Assert.DoesNotContain(thread.DomainEvents, e => e is GlobalChatThreadStarted);
    }

    [Fact]
    public void SourceReference_AcceptsPersonEntityType()
    {
        var personId = Guid.NewGuid();
        var reference = new SourceReference(SourceReferenceEntityType.Person, personId, "Jane Doe");
        Assert.Equal(SourceReferenceEntityType.Person, reference.EntityType);
        Assert.Equal(personId, reference.EntityId);
    }

    [Fact]
    public void SourceReference_RejectsUnknownEntityType()
    {
        var unknown = (SourceReferenceEntityType)9999;
        Assert.Throws<ArgumentException>(() => new SourceReference(unknown, Guid.NewGuid()));
    }

    [Fact]
    public void SourceReference_AcceptsForwardCompatibleEntityTypes()
    {
        // These are reserved for people-lens and must validate today even though no records exist.
        var id = Guid.NewGuid();
        Assert.Equal(SourceReferenceEntityType.Observation, new SourceReference(SourceReferenceEntityType.Observation, id).EntityType);
        Assert.Equal(SourceReferenceEntityType.Goal, new SourceReference(SourceReferenceEntityType.Goal, id).EntityType);
        Assert.Equal(SourceReferenceEntityType.OneOnOne, new SourceReference(SourceReferenceEntityType.OneOnOne, id).EntityType);
    }
}
