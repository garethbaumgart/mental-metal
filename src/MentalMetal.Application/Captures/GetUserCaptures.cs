using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Captures;

public sealed class GetUserCapturesHandler(
    ICaptureRepository captureRepository,
    ICurrentUserService currentUserService)
{
    public async Task<List<CaptureResponse>> HandleAsync(
        CaptureType? typeFilter,
        ProcessingStatus? statusFilter,
        CancellationToken cancellationToken,
        bool includeTriaged = false)
    {
        var captures = await captureRepository.GetAllAsync(
            currentUserService.UserId, typeFilter, statusFilter, cancellationToken, includeTriaged);

        return captures.Select(CaptureResponse.From).ToList();
    }
}
