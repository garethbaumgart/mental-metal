using MentalMetal.Application.Captures;
using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.DailyCloseOut;

public sealed class ReassignCaptureHandler(
    ICaptureRepository captureRepository,
    IPersonRepository personRepository,
    IInitiativeRepository initiativeRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<CloseOutQueueItem> HandleAsync(
        Guid captureId, ReassignCaptureRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = currentUserService.UserId;
        var capture = (await captureRepository.GetByIdAsync(captureId, cancellationToken))
            .EnsureOwned(userId, captureId);

        var targetPeople = (request.PersonIds ?? []).Distinct().ToList();
        var targetInitiatives = (request.InitiativeIds ?? []).Distinct().ToList();

        if (targetPeople.Count > 0)
        {
            var people = await personRepository.GetByIdsAsync(userId, targetPeople, cancellationToken);
            if (people.Count != targetPeople.Count)
            {
                var found = people.Select(p => p.Id).ToList();
                var missing = targetPeople.Except(found).ToList();
                throw new ArgumentException(
                    $"Unknown or unauthorised person id(s): {string.Join(", ", missing)}");
            }
        }

        if (targetInitiatives.Count > 0)
        {
            foreach (var initiativeId in targetInitiatives)
            {
                var initiative = await initiativeRepository.GetByIdAsync(initiativeId, cancellationToken);
                if (initiative is null || initiative.UserId != userId)
                    throw new ArgumentException(
                        $"Unknown or unauthorised initiative id: {initiativeId}");
            }
        }

        // Diff people
        var currentPeople = capture.LinkedPersonIds.ToList();
        foreach (var add in targetPeople.Except(currentPeople))
            capture.LinkToPerson(add);
        foreach (var remove in currentPeople.Except(targetPeople))
            capture.UnlinkFromPerson(remove);

        // Diff initiatives
        var currentInitiatives = capture.LinkedInitiativeIds.ToList();
        foreach (var add in targetInitiatives.Except(currentInitiatives))
            capture.LinkToInitiative(add);
        foreach (var remove in currentInitiatives.Except(targetInitiatives))
            capture.UnlinkFromInitiative(remove);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CloseOutQueueItem.From(capture);
    }
}
