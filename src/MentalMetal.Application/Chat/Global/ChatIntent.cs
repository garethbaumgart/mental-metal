namespace MentalMetal.Application.Chat.Global;

/// <summary>
/// Coarse buckets the global-chat IntentClassifier maps user questions onto. The
/// GlobalChatContextBuilder pulls a per-intent context slice based on these.
/// </summary>
public enum ChatIntent
{
    MyDay,
    MyWeek,
    OverdueWork,
    PersonLens,
    InitiativeStatus,
    CaptureSearch,
    Generic,
}

/// <summary>
/// Hint range for capture / time-window intents. Inclusive on both ends.
/// </summary>
public sealed record DateRangeHint
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    public DateRangeHint(DateOnly start, DateOnly end)
    {
        if (end < start)
            throw new ArgumentException("End date must be on or after start date.", nameof(end));
        Start = start;
        End = end;
    }
}

/// <summary>
/// Resolved entity hints. The classifier resolves names against the user's actual
/// records so the context builder can pull targeted records (avoids second name lookup).
/// </summary>
public sealed record EntityHints(
    IReadOnlyList<Guid> PersonIds,
    IReadOnlyList<Guid> InitiativeIds,
    DateRangeHint? DateRange)
{
    public static EntityHints Empty { get; } = new([], [], null);
}

/// <summary>
/// The output of an IIntentClassifier — one or more intents (rare to have more than two)
/// plus any resolved hints. A returned set with only Generic indicates "fallback" path.
/// </summary>
public sealed record IntentSet(IReadOnlyList<ChatIntent> Intents, EntityHints Hints)
{
    public bool IsGenericOnly => Intents.Count == 1 && Intents[0] == ChatIntent.Generic;

    public static IntentSet Generic { get; } = new([ChatIntent.Generic], EntityHints.Empty);
}
