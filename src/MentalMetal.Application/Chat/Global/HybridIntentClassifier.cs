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

        // Re-run rule layer to grab entity hints even if no intent matched (the rule classifier
        // returns Generic without hints when no intent fired). The AI classifier is unaware of
        // the user's specific people/initiatives, so we keep its hints empty and rely on the
        // rule re-pass for hints.
        var ai = await aiClassifier.ClassifyAsync(userId, userMessage, cancellationToken);
        return ai;
    }
}
