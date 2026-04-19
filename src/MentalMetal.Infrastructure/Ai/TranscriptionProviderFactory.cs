using MentalMetal.Application.Captures;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.Options;

namespace MentalMetal.Infrastructure.Ai;

public sealed class TranscriptionProviderFactory(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IApiKeyEncryptionService encryptionService,
    IOptions<DeepgramSettings> deepgramSettings,
    IHttpClientFactory httpClientFactory) : ITranscriptionProviderFactory
{
    public async Task<IAudioTranscriptionProvider> CreateAsync(CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(
            currentUserService.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user not found.");

        var config = user.TranscriptionProviderConfig
            ?? throw new AudioTranscriptionUnavailableException(
                "Transcription provider not configured. Add your Deepgram API key in Settings.");

        var apiKey = encryptionService.Decrypt(config.EncryptedApiKey);
        var settings = deepgramSettings.Value;

        return new DeepgramAudioTranscriptionProvider(apiKey, config.Model, settings, httpClientFactory);
    }
}
