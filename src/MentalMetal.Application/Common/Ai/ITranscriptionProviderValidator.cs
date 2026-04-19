using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Common.Ai;

public interface ITranscriptionProviderValidator
{
    Task<bool> ValidateAsync(
        TranscriptionProvider provider,
        string apiKey,
        CancellationToken cancellationToken);
}
