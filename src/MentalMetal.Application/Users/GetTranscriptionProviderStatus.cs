using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Users;

public sealed record TranscriptionProviderStatusResponse(
    bool IsConfigured,
    string? Provider,
    string? Model);

public sealed class GetTranscriptionProviderStatusHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUserService)
{
    public async Task<TranscriptionProviderStatusResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(
            currentUserService.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user not found.");

        var config = user.TranscriptionProviderConfig;

        return new TranscriptionProviderStatusResponse(
            IsConfigured: config is not null,
            Provider: config?.Provider.ToString(),
            Model: config?.Model);
    }
}
