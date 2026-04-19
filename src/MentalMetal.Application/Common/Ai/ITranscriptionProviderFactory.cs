namespace MentalMetal.Application.Common.Ai;

public interface ITranscriptionProviderFactory
{
    Task<IAudioTranscriptionProvider> CreateAsync(CancellationToken cancellationToken);
}
