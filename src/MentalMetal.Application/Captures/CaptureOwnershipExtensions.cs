using MentalMetal.Domain.Captures;

namespace MentalMetal.Application.Captures;

internal static class CaptureOwnershipExtensions
{
    /// <summary>
    /// Returns the capture if it exists and belongs to the given user,
    /// otherwise throws InvalidOperationException (treated as not-found by endpoints).
    /// </summary>
    public static Capture EnsureOwned(this Capture? capture, Guid userId, Guid captureId)
    {
        if (capture is null || capture.UserId != userId)
            throw new InvalidOperationException($"Capture not found: {captureId}");

        return capture;
    }
}
