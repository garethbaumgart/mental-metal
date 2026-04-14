using MentalMetal.Application.Captures;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Options;

namespace MentalMetal.Web.Features.Captures;

public static class AudioCaptureEndpoints
{
    public static IEndpointRouteBuilder MapAudioCaptureEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/captures/audio", async (
            HttpRequest httpRequest,
            IOptions<AudioUploadOptions> optionsAccessor,
            UploadAudioCaptureHandler handler,
            CancellationToken cancellationToken) =>
        {
            var options = optionsAccessor.Value;

            if (!httpRequest.HasFormContentType)
                return Results.BadRequest(new { error = "Expected multipart/form-data." });

            var form = await httpRequest.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "Missing 'file' field." });

            if (file.Length > options.MaxSizeBytes)
                return Results.BadRequest(new { errorCode = AudioCaptureErrorCodes.AudioTooLarge });

            if (!options.AllowedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                return Results.BadRequest(new { errorCode = AudioCaptureErrorCodes.AudioInvalidFormat });

            var title = form["title"].ToString();
            var source = form["source"].ToString();
            var durationStr = form["durationSeconds"].ToString();
            double duration = 0;
            if (!string.IsNullOrWhiteSpace(durationStr) && double.TryParse(durationStr, out var parsed))
                duration = parsed;

            try
            {
                await using var stream = file.OpenReadStream();
                var response = await handler.HandleAsync(
                    new UploadAudioCaptureRequest(
                        stream, file.ContentType, duration,
                        string.IsNullOrWhiteSpace(title) ? null : title,
                        string.IsNullOrWhiteSpace(source) ? null : source),
                    cancellationToken);
                return Results.Created($"/api/captures/{response.Id}", response);
            }
            catch (AudioCaptureException ex)
            {
                return Results.BadRequest(new { errorCode = ex.ErrorCode, message = ex.Message });
            }
        })
        .RequireAuthorization()
        .DisableAntiforgery()
        .WithMetadata(new RequestSizeLimitAttribute(500_000_000)); // outer cap; options enforces tighter limit

        app.MapPost("/api/captures/{id:guid}/transcribe", async (
            Guid id,
            TranscribeCaptureHandler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await handler.HandleAsync(id, cancellationToken);
                return Results.Ok(response);
            }
            catch (AudioCaptureException ex) when (ex.ErrorCode == AudioCaptureErrorCodes.CaptureNotFound)
            {
                return Results.NotFound(new { errorCode = ex.ErrorCode });
            }
            catch (AudioCaptureException ex)
            {
                return Results.BadRequest(new { errorCode = ex.ErrorCode, message = ex.Message });
            }
        }).RequireAuthorization();

        app.MapGet("/api/captures/{id:guid}/transcript", async (
            Guid id,
            GetCaptureTranscriptHandler handler,
            CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(id, cancellationToken);
            if (response is null)
                return Results.NotFound(new { errorCode = AudioCaptureErrorCodes.CaptureNotFound });
            return Results.Ok(response);
        }).RequireAuthorization();

        app.MapPatch("/api/captures/{id:guid}/speakers", async (
            Guid id,
            UpdateCaptureSpeakersRequest request,
            UpdateCaptureSpeakersHandler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await handler.HandleAsync(id, request, cancellationToken);
                return Results.Ok(response);
            }
            catch (AudioCaptureException ex) when (ex.ErrorCode == AudioCaptureErrorCodes.CaptureNotFound)
            {
                return Results.NotFound(new { errorCode = ex.ErrorCode });
            }
            catch (AudioCaptureException ex)
            {
                return Results.BadRequest(new { errorCode = ex.ErrorCode, message = ex.Message });
            }
        }).RequireAuthorization();

        return app;
    }
}

internal class RequestSizeLimitAttribute(long bytes) : Attribute, IRequestSizeLimitMetadata
{
    public long? MaxRequestBodySize { get; } = bytes;
}
