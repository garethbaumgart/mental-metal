using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Users;

public sealed class ConfigureAiProviderHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IApiKeyEncryptionService encryptionService,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(ConfigureAiProviderRequest request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(
            currentUserService.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user not found.");

        if (!Enum.TryParse<AiProvider>(request.Provider, ignoreCase: true, out var provider))
            throw new ArgumentException($"Unsupported AI provider: {request.Provider}");

        var encryptedKey = encryptionService.Encrypt(request.ApiKey);

        user.ConfigureAiProvider(provider, encryptedKey, request.Model, request.MaxTokens);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
