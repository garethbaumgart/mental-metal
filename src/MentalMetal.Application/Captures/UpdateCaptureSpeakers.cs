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
        var capture = await captureRepository.GetByIdWithTranscriptAsync(captureId, cancellationToken);
        if (capture is null || capture.UserId != userId)
            throw new AudioCaptureException(AudioCaptureErrorCodes.CaptureNotFound);

        // Null or empty mapping set is a no-op (per spec "Empty mapping set is a no-op").
        var incoming = request.Mappings ?? Array.Empty<SpeakerMapping>();
        if (incoming.Count == 0)
            return CaptureResponse.From(capture);

        // Reject duplicate speaker labels — the domain mapping is label→person and duplicates
        // in the request would silently last-write-wins.
        var duplicateLabel = incoming
            .GroupBy(m => m.SpeakerLabel, StringComparer.Ordinal)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateLabel is not null)
            throw new AudioCaptureException(
                AudioCaptureErrorCodes.SpeakerLabelNotFound,
                $"Duplicate mapping for speaker label: {duplicateLabel.Key}");

        // Verify every PersonId exists for this user.
        var personIds = incoming.Select(m => m.PersonId).Distinct().ToList();
        var people = await personRepository.GetByIdsAsync(userId, personIds, cancellationToken);
        if (people.Count != personIds.Count)
            throw new AudioCaptureException(AudioCaptureErrorCodes.SpeakerPersonNotFound);

        var mapping = incoming.ToDictionary(m => m.SpeakerLabel, m => m.PersonId, StringComparer.Ordinal);

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
