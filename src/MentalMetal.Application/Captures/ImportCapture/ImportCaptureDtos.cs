using MentalMetal.Domain.Captures;

namespace MentalMetal.Application.Captures.ImportCapture;

public sealed record ImportCaptureFromJsonRequest(
    CaptureType Type,
    string Content,
    string? SourceUrl = null,
    string? Title = null,
    DateTimeOffset? MeetingAt = null);

public sealed record ImportCaptureFromFileRequest(
    Stream FileStream,
    string ContentType,
    string FileName,
    CaptureType? Type = null,
    string? SourceUrl = null,
    string? Title = null,
    DateTimeOffset? MeetingAt = null);

public sealed record ImportCaptureResponse(Guid Id);
