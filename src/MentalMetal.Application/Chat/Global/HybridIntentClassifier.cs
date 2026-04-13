namespace MentalMetal.Application.Chat.Global;

/// <summary>
/// Runs the cheap rule classifier first; only invokes the AI classifier when rules return
/// the Generic fallback. Rule-resolved entity hints are preserved across the AI fallback so
/// a question like "Anything I should be worried about with Jane?" still pulls Jane's records
/// even though the rule layer didn't fire on the verb.
/// </summary>
public sealed class HybridIntentClassifier(
    RuleIntentClassifier ruleClassifier,
    AiIntentClassifier aiClassifier) : IIntentClassifier
{
    public async Task<IntentSet> ClassifyAsync(Guid userId, string userMessage, CancellationToken cancellationToken)
    {
        var rules = await ruleClassifier.ClassifyAsync(userId, userMessage, cancellationToken);
        if (!rules.IsGenericOnly)
            return rules;

        // Re-run the rule-layer name resolver to grab entity hints even if no intent keyword
        // matched. The AI classifier is unaware of the user's specific people/initiatives, so
        // we take AI-classified intents and merge in rule-resolved Person/Initiative hints.
        var ai = await aiClassifier.ClassifyAsync(userId, userMessage, cancellationToken);
        var ruleHints = await ruleClassifier.ResolveEntityHintsAsync(userId, userMessage, cancellationToken);

        var mergedPersonIds = ai.Hints.PersonIds.Concat(ruleHints.PersonIds).Distinct().ToList();
        var mergedInitiativeIds = ai.Hints.InitiativeIds.Concat(ruleHints.InitiativeIds).Distinct().ToList();
        var mergedDateRange = ai.Hints.DateRange ?? ruleHints.DateRange;

        return ai with { Hints = new EntityHints(mergedPersonIds, mergedInitiativeIds, mergedDateRange) };
    }
}
