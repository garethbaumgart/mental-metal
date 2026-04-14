using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Captures;

public sealed record SpeakerMapping(string SpeakerLabel, Guid PersonId);
public sealed record UpdateCaptureSpeakersRequest(IReadOnlyList<SpeakerMapping> Mappings);

public sealed class UpdateCaptureSpeakersHandler(
    ICaptureRepository captureRepository,
    IPersonRepository personRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<CaptureResponse> HandleAsync(
        Guid captureId, UpdateCaptureSpeakersRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = currentUserService.UserId;
        var capture = await captureRepository.GetByIdAsync(captureId, cancellationToken);
        if (capture is null || capture.UserId != userId)
            throw new AudioCaptureException(AudioCaptureErrorCodes.CaptureNotFound);

        if (request.Mappings.Count == 0)
            return CaptureResponse.From(capture);

        // Verify every PersonId exists for this user.
        var personIds = request.Mappings.Select(m => m.PersonId).Distinct().ToList();
        var people = await personRepository.GetByIdsAsync(userId, personIds, cancellationToken);
        if (people.Count != personIds.Count)
            throw new AudioCaptureException(AudioCaptureErrorCodes.SpeakerPersonNotFound);

        var mapping = request.Mappings.ToDictionary(m => m.SpeakerLabel, m => m.PersonId, StringComparer.Ordinal);

        try
        {
            capture.IdentifySpeakers(mapping, timeProvider.GetUtcNow());
        }
        catch (KeyNotFoundException ex)
        {
            throw new AudioCaptureException(AudioCaptureErrorCodes.SpeakerLabelNotFound, ex.Message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CaptureResponse.From(capture);
    }
}
