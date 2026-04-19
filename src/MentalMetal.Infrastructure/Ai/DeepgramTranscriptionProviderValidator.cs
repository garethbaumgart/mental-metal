using System.Net.Http.Headers;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.Options;

namespace MentalMetal.Infrastructure.Ai;

public sealed class DeepgramTranscriptionProviderValidator(
    IHttpClientFactory httpClientFactory,
    IOptions<DeepgramSettings> deepgramSettings) : ITranscriptionProviderValidator
{
    public async Task<bool> ValidateAsync(
        TranscriptionProvider provider,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var settings = deepgramSettings.Value;
        var baseUrl = settings.BaseUrl;
        var httpScheme = IsLoopbackAddress(baseUrl) ? "http" : "https";

        using var httpClient = httpClientFactory.CreateClient("Deepgram");
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Token", apiKey);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            timeoutCts.Token, cancellationToken);

        var response = await httpClient.GetAsync(
            $"{httpScheme}://{baseUrl}/v1/projects", linkedCts.Token);

        return response.IsSuccessStatusCode;
    }

    private static bool IsLoopbackAddress(string baseUrl)
    {
        return baseUrl.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
            || baseUrl.StartsWith("127.", StringComparison.Ordinal)
            || baseUrl.StartsWith("[::1]", StringComparison.Ordinal)
            || baseUrl.Equals("::1", StringComparison.Ordinal);
    }
}
