using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Users;

public sealed record ValidateTranscriptionProviderRequest(
    string Provider,
    string ApiKey);

public sealed record ValidateTranscriptionProviderResponse(
    bool Success,
    string? Error);

public sealed class ValidateTranscriptionProviderHandler(
    ITranscriptionProviderValidator providerValidator)
{
    public async Task<ValidateTranscriptionProviderResponse> HandleAsync(
        ValidateTranscriptionProviderRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TranscriptionProvider>(request.Provider, ignoreCase: true, out var provider)
            || !Enum.IsDefined(provider))
            return new ValidateTranscriptionProviderResponse(false, $"Unsupported provider: {request.Provider}");

        if (string.IsNullOrWhiteSpace(request.ApiKey))
            return new ValidateTranscriptionProviderResponse(false, "API key is required.");

        try
        {
            var valid = await providerValidator.ValidateAsync(provider, request.ApiKey, cancellationToken);
            return valid
                ? new ValidateTranscriptionProviderResponse(true, null)
                : new ValidateTranscriptionProviderResponse(false, "Invalid API key.");
        }
        catch (Exception)
        {
            return new ValidateTranscriptionProviderResponse(false,
                "Unable to connect to the transcription provider. Please check your API key and try again.");
        }
    }
}
