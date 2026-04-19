using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using MentalMetal.Application.Captures;
using MentalMetal.Application.Common.Ai;

namespace MentalMetal.Infrastructure.Ai;

public sealed class DeepgramAudioTranscriptionProvider(
    string apiKey,
    string model,
    DeepgramSettings settings,
    IHttpClientFactory httpClientFactory) : IAudioTranscriptionProvider
{
    public async Task<AudioTranscriptionResult> TranscribeAsync(
        AudioTranscriptionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var baseUrl = settings.BaseUrl;
        var httpScheme = IsLoopbackAddress(baseUrl) ? "http" : "https";

        var queryParams = new List<string>
        {
            $"model={Uri.EscapeDataString(model)}",
            $"punctuate={settings.Punctuate.ToString().ToLowerInvariant()}",
            $"diarize={settings.Diarize.ToString().ToLowerInvariant()}",
            $"language={Uri.EscapeDataString(settings.Language)}",
            "paragraphs=true",
        };

        var url = $"{httpScheme}://{baseUrl}/v1/listen?{string.Join("&", queryParams)}";

        using var httpClient = httpClientFactory.CreateClient("Deepgram");
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Token", apiKey);

        using var content = new StreamContent(request.AudioStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(request.MimeType);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync(url, content, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new AudioTranscriptionUnavailableException(
                "Cannot reach Deepgram transcription service.", ex);
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new AudioTranscriptionUnavailableException(
                "Deepgram API key is invalid or does not have the required permissions.");
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = JsonDocument.Parse(json);

        var fullText = "";
        var segments = new List<AudioTranscriptSegmentDto>();

        var channels = doc.RootElement
            .GetProperty("results")
            .GetProperty("channels");

        if (channels.GetArrayLength() > 0)
        {
            var alternatives = channels[0].GetProperty("alternatives");
            if (alternatives.GetArrayLength() > 0)
            {
                var alt = alternatives[0];
                fullText = alt.GetProperty("transcript").GetString() ?? "";

                // Build segments by grouping consecutive words by speaker
                if (alt.TryGetProperty("words", out var wordsElement))
                {
                    segments = GroupWordsBySpeaker(wordsElement);
                }
            }
        }

        return new AudioTranscriptionResult(fullText, segments);
    }

    private static List<AudioTranscriptSegmentDto> GroupWordsBySpeaker(JsonElement wordsElement)
    {
        var segments = new List<AudioTranscriptSegmentDto>();
        if (wordsElement.GetArrayLength() == 0)
            return segments;

        var currentSpeaker = -1;
        var currentStart = 0.0;
        var currentEnd = 0.0;
        var currentWords = new List<string>();

        foreach (var word in wordsElement.EnumerateArray())
        {
            var speaker = word.TryGetProperty("speaker", out var sp) ? sp.GetInt32() : 0;
            var wordText = word.GetProperty("word").GetString() ?? "";
            var start = word.GetProperty("start").GetDouble();
            var end = word.GetProperty("end").GetDouble();

            if (speaker != currentSpeaker && currentWords.Count > 0)
            {
                segments.Add(new AudioTranscriptSegmentDto(
                    currentStart,
                    currentEnd,
                    $"Speaker {currentSpeaker}",
                    string.Join(" ", currentWords)));
                currentWords.Clear();
            }

            if (currentWords.Count == 0)
            {
                currentSpeaker = speaker;
                currentStart = start;
            }

            currentWords.Add(wordText);
            currentEnd = end;
        }

        if (currentWords.Count > 0)
        {
            segments.Add(new AudioTranscriptSegmentDto(
                currentStart,
                currentEnd,
                $"Speaker {currentSpeaker}",
                string.Join(" ", currentWords)));
        }

        return segments;
    }

    private static bool IsLoopbackAddress(string baseUrl)
    {
        return baseUrl.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
            || baseUrl.StartsWith("127.", StringComparison.Ordinal)
            || baseUrl.StartsWith("[::1]", StringComparison.Ordinal)
            || baseUrl.Equals("::1", StringComparison.Ordinal);
    }
}
