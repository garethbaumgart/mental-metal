using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.ChatThreads;

/// <summary>
/// Value object identifying the scope binding for a ChatThread.
/// For the initiative-ai-chat capability only the Initiative variant is produced.
/// Global is reserved for the forthcoming global-ai-chat capability so the aggregate
/// does not need to split.
/// </summary>
public sealed class ContextScope : ValueObject
{
    public ContextScopeType Type { get; }
    public Guid? InitiativeId { get; }

    private ContextScope(ContextScopeType type, Guid? initiativeId)
    {
        Type = type;
        InitiativeId = initiativeId;
    }

    public static ContextScope Initiative(Guid initiativeId)
    {
        if (initiativeId == Guid.Empty)
            throw new ArgumentException("InitiativeId is required for Initiative context scope.", nameof(initiativeId));
        return new ContextScope(ContextScopeType.Initiative, initiativeId);
    }

    /// <summary>
    /// Reserved for global-ai-chat. Not produced by initiative-ai-chat handlers but the
    /// aggregate tolerates the type so a future capability need not fork the model.
    /// </summary>
    public static ContextScope Global() => new(ContextScopeType.Global, null);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return InitiativeId;
    }
}
