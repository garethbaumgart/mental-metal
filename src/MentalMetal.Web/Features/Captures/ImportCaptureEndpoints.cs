using MentalMetal.Application.Captures.AutoExtract;
using MentalMetal.Application.Captures.ImportCapture;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Users;

namespace MentalMetal.Web.Features.Captures;

public static class ImportCaptureEndpoints
{
    public static IEndpointRouteBuilder MapImportCaptureEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/captures/import", async (
            HttpRequest httpRequest,
            ImportCaptureFromJsonHandler jsonHandler,
            ImportCaptureFromFileHandler fileHandler,
            BackgroundExtractionTrigger extractionTrigger,
            ICurrentUserService currentUser,
            CancellationToken cancellationToken) =>
        {
            try
            {
                IResult result;
                Guid? captureId = null;

                if (httpRequest.HasFormContentType)
                {
                    var (res, id) = await HandleMultipartAsync(httpRequest, fileHandler, cancellationToken);
                    result = res;
                    captureId = id;
                }
                else if (!httpRequest.HasJsonContentType())
                {
                    return Results.Problem(
                        detail: "Expected application/json or multipart/form-data.",
                        statusCode: StatusCodes.Status415UnsupportedMediaType);
                }
                else
                {
                    var (res, id) = await HandleJsonAsync(httpRequest, jsonHandler, cancellationToken);
                    result = res;
                    captureId = id;
                }

                // Fire extraction in the background — response returns immediately
                if (captureId.HasValue && captureId.Value != Guid.Empty)
                    extractionTrigger.FireAndForget(captureId.Value, currentUser.UserId);

                return result;
            }
            catch (UnsupportedMediaTypeException ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status415UnsupportedMediaType);
            }
            catch (PayloadTooLargeException ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status413PayloadTooLarge);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .RequireAuthorization("RequireCapturesWriteScope")
        .RequireCors("ImportIngestFromGoogle")
        .DisableAntiforgery();

        return app;
    }

    private static async Task<(IResult Result, Guid CaptureId)> HandleJsonAsync(
        HttpRequest httpRequest,
        ImportCaptureFromJsonHandler handler,
        CancellationToken cancellationToken)
    {
        var body = await httpRequest.ReadFromJsonAsync<ImportJsonBody>(cancellationToken);
        if (body is null)
            return (Results.BadRequest(new { error = "Invalid JSON body." }), Guid.Empty);

        if (!Enum.TryParse<CaptureType>(body.Type, ignoreCase: true, out var captureType)
            || captureType is not (CaptureType.Transcript or CaptureType.QuickNote))
            return (Results.BadRequest(new { error = $"Unsupported type: {body.Type}. Must be 'Transcript' or 'QuickNote'." }), Guid.Empty);

        var request = new ImportCaptureFromJsonRequest(
            captureType,
            body.Content ?? "",
            body.SourceUrl,
            body.Title);

        var result = await handler.HandleAsync(request, cancellationToken);
        return (Results.Created($"/api/captures/{result.Id}", result), result.Id);
    }

    private static async Task<(IResult Result, Guid CaptureId)> HandleMultipartAsync(
        HttpRequest httpRequest,
        ImportCaptureFromFileHandler handler,
        CancellationToken cancellationToken)
    {
        var form = await httpRequest.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
            return (Results.BadRequest(new { error = "Missing 'file' field." }), Guid.Empty);

        CaptureType? captureType = null;
        var typeStr = form["type"].FirstOrDefault();
        if (!string.IsNullOrEmpty(typeStr))
        {
            if (!Enum.TryParse<CaptureType>(typeStr, ignoreCase: true, out var parsed))
                return (Results.BadRequest(new { error = $"Unsupported type: {typeStr}." }), Guid.Empty);
            captureType = parsed;
        }

        await using var stream = file.OpenReadStream();
        var request = new ImportCaptureFromFileRequest(
            stream,
            file.ContentType,
            file.FileName,
            file.Length,
            captureType,
            form["sourceUrl"].FirstOrDefault(),
            form["title"].FirstOrDefault());

        var result = await handler.HandleAsync(request, cancellationToken);
        return (Results.Created($"/api/captures/{result.Id}", result), result.Id);
    }

    private sealed record ImportJsonBody(
        string? Type,
        string? Content,
        string? SourceUrl,
        string? Title);
}
