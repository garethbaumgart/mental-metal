using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Common.Ai;

public class AiProviderException(
    AiProvider provider,
    int? statusCode,
    string message) : Exception(message)
{
    public AiProvider Provider { get; } = provider;
    public int? StatusCode { get; } = statusCode;
}

public class TasteLimitExceededException()
    : Exception("Daily free AI operation limit reached. Add your own AI provider key for unlimited access.");
