using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Captures.ImportCapture;

public sealed class ImportCaptureFromFileHandler(
    ICaptureRepository captureRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IEnumerable<ITranscriptFileParser> parsers)
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly HashSet<string> SupportedContentTypes =
    [
        "text/plain",
        "text/html",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/octet-stream",
    ];

    private static readonly HashSet<string> SupportedExtensions =
        [".txt", ".html", ".htm", ".docx"];

    public async Task<ImportCaptureResponse> HandleAsync(
        ImportCaptureFromFileRequest request, CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(request.FileName).ToLower();
        if (!SupportedExtensions.Contains(ext) && !SupportedContentTypes.Contains(request.ContentType))
            throw new UnsupportedMediaTypeException(
                $"Unsupported file type: {request.ContentType} / {ext}");

        if (request.FileLength > MaxFileSizeBytes)
            throw new PayloadTooLargeException("File exceeds the maximum allowed size of 10 MB.");

        var parser = parsers.FirstOrDefault(p => p.CanHandle(request.ContentType, request.FileName))
            ?? throw new UnsupportedMediaTypeException(
                $"No parser available for: {request.ContentType} / {request.FileName}");

        string content;
        try
        {
            content = await parser.ExtractTextAsync(request.FileStream, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ArgumentException($"Failed to parse file: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Parsed file content is empty.");

        var detected = TranscriptFormatDetector.Detect(content);

        var captureType = request.Type ?? InferType(ext);
        if (captureType is not (CaptureType.Transcript or CaptureType.QuickNote))
            throw new ArgumentException($"Unsupported capture type for file import: {captureType}");

        var capture = Capture.Create(
            currentUserService.UserId,
            detected.NormalizedContent,
            captureType,
            request.SourceUrl,
            request.Title);

        await captureRepository.AddAsync(capture, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImportCaptureResponse(capture.Id);
    }

    private static CaptureType InferType(string extension) =>
        extension is ".docx" or ".html" or ".htm"
            ? CaptureType.Transcript
            : CaptureType.QuickNote;
}

public sealed class UnsupportedMediaTypeException(string message) : Exception(message);
public sealed class PayloadTooLargeException(string message) : Exception(message);
