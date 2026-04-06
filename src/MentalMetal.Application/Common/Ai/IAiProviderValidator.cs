using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Common.Ai;

public interface IAiProviderValidator
{
    Task<string> ValidateAsync(
        AiProvider provider,
        string apiKey,
        string model,
        CancellationToken cancellationToken);
}
