using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Nudges;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Nudges;

public sealed class UpdateNudgeHandler(
    INudgeRepository nudgeRepository,
    IPersonRepository personRepository,
    IInitiativeRepository initiativeRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<NudgeResponse> HandleAsync(Guid id, UpdateNudgeRequest request, CancellationToken ct)
    {
        var userId = currentUserService.UserId;

        var nudge = await nudgeRepository.GetByIdAsync(id, ct);
        if (nudge is null || nudge.UserId != userId)
            throw new NotFoundException("Nudge", id);

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

        nudge.UpdateDetails(request.Title, request.Notes, request.PersonId, request.InitiativeId);
        await unitOfWork.SaveChangesAsync(ct);

        return NudgeResponse.From(nudge);
    }
}
