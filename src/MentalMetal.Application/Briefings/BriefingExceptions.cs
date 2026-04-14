namespace MentalMetal.Application.Briefings;

/// <summary>
/// Thrown when a briefing is requested but the user has not configured an AI provider
/// and no taste-key fallback is available. The Web layer maps this to HTTP 409 with
/// error code <c>ai_provider_not_configured</c>.
/// </summary>
public sealed class AiProviderNotConfiguredException(string message) : InvalidOperationException(message);
