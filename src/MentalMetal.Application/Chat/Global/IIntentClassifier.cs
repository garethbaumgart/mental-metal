namespace MentalMetal.Application.Chat.Global;

/// <summary>
/// Classifies a user message into an <see cref="IntentSet"/>. The hybrid implementation
/// runs deterministic rules first, falling back to an AI classifier only when rules
/// produce no match (or only the Generic fallback).
/// </summary>
public interface IIntentClassifier
{
    Task<IntentSet> ClassifyAsync(Guid userId, string userMessage, CancellationToken cancellationToken);
}
