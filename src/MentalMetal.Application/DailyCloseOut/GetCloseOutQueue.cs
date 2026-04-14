using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.DailyCloseOut;

public sealed class GetCloseOutQueueHandler(
    ICaptureRepository captureRepository,
    ICurrentUserService currentUserService)
{
    public async Task<CloseOutQueueResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var captures = await captureRepository.GetCloseOutQueueAsync(
            currentUserService.UserId, cancellationToken);

        var items = captures.Select(CloseOutQueueItem.From).ToList();
        var counts = new CloseOutQueueCounts(
            Total: items.Count,
            Raw: items.Count(i => i.ProcessingStatus == ProcessingStatus.Raw),
            Processing: items.Count(i => i.ProcessingStatus == ProcessingStatus.Processing),
            Processed: items.Count(i => i.ProcessingStatus == ProcessingStatus.Processed),
            Failed: items.Count(i => i.ProcessingStatus == ProcessingStatus.Failed));

        return new CloseOutQueueResponse(items, counts);
    }
}
