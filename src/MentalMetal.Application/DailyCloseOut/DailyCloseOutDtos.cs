using MentalMetal.Application.Captures;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.DailyCloseOut;

public sealed record CloseOutQueueItem(
    Guid Id,
    string RawContent,
    CaptureType CaptureType,
    ProcessingStatus ProcessingStatus,
    ExtractionStatus ExtractionStatus,
    bool ExtractionResolved,
    AiExtractionResponse? AiExtraction,
    string? FailureReason,
    IReadOnlyList<Guid> LinkedPersonIds,
    IReadOnlyList<Guid> LinkedInitiativeIds,
    string? Title,
    DateTimeOffset CapturedAt,
    DateTimeOffset? ProcessedAt)
{
    public static CloseOutQueueItem From(Capture capture) => new(
        capture.Id,
        capture.RawContent,
        capture.CaptureType,
        capture.ProcessingStatus,
        capture.ExtractionStatus,
        capture.ExtractionResolved,
        AiExtractionResponse.From(capture.AiExtraction),
        capture.FailureReason,
        capture.LinkedPersonIds.ToList(),
        capture.LinkedInitiativeIds.ToList(),
        capture.Title,
        capture.CapturedAt,
        capture.ProcessedAt);
}

public sealed record CloseOutQueueCounts(int Total, int Raw, int Processing, int Processed, int Failed);

public sealed record CloseOutQueueResponse(
    IReadOnlyList<CloseOutQueueItem> Items,
    CloseOutQueueCounts Counts);

public sealed record ReassignCaptureRequest(
    IReadOnlyList<Guid>? PersonIds,
    IReadOnlyList<Guid>? InitiativeIds);

public sealed record CloseOutDayRequest(DateOnly? Date);

public sealed record DailyCloseOutLogDto(
    Guid Id,
    DateOnly Date,
    DateTimeOffset ClosedAtUtc,
    int ConfirmedCount,
    int DiscardedCount,
    int RemainingCount)
{
    public static DailyCloseOutLogDto From(DailyCloseOutLog log) => new(
        log.Id,
        log.Date,
        log.ClosedAtUtc,
        log.ConfirmedCount,
        log.DiscardedCount,
        log.RemainingCount);
}
