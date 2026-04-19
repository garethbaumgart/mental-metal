using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Users;

public sealed record ConfigureTranscriptionProviderRequest(
    string Provider,
    string ApiKey,
    string Model);

public sealed class ConfigureTranscriptionProviderHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IApiKeyEncryptionService encryptionService,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(ConfigureTranscriptionProviderRequest request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(
            currentUserService.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user not found.");

        if (!Enum.TryParse<TranscriptionProvider>(request.Provider, ignoreCase: true, out var provider)
            || !Enum.IsDefined(provider))
            throw new ArgumentException($"Unsupported transcription provider: {request.Provider}");

        ArgumentException.ThrowIfNullOrWhiteSpace(request.ApiKey, nameof(request.ApiKey));

        var encryptedKey = encryptionService.Encrypt(request.ApiKey);

        user.ConfigureTranscriptionProvider(provider, encryptedKey, request.Model);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
