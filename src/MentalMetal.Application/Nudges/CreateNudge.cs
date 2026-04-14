using MentalMetal.Application.Common;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Nudges;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Nudges;

public sealed class CreateNudgeHandler(
    INudgeRepository nudgeRepository,
    IPersonRepository personRepository,
    IInitiativeRepository initiativeRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<NudgeResponse> HandleAsync(CreateNudgeRequest request, CancellationToken ct)
    {
        var userId = currentUserService.UserId;

        // Validate optional links belong to current user.
        if (request.PersonId is { } personId)
        {
            var person = await personRepository.GetByIdAsync(personId, ct);
            if (person is null || person.UserId != userId)
                throw new LinkedEntityNotFoundException($"Person '{personId}' not found for current user.");
        }

        if (request.InitiativeId is { } initiativeId)
        {
            var initiative = await initiativeRepository.GetByIdAsync(initiativeId, ct);
            if (initiative is null || initiative.UserId != userId)
                throw new LinkedEntityNotFoundException($"Initiative '{initiativeId}' not found for current user.");
        }

        var cadence = NudgeCadence.FromRequest(
            request.CadenceType,
            request.CustomIntervalDays,
            request.DayOfWeek,
            request.DayOfMonth);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var nudge = Nudge.Create(
            userId,
            request.Title,
            cadence,
            today,
            request.StartDate,
            request.PersonId,
            request.InitiativeId,
            request.Notes);

        await nudgeRepository.AddAsync(nudge, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return NudgeResponse.From(nudge);
    }
}
