using MentalMetal.Application.Common;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives;

public sealed class LinkPersonHandler(
    IInitiativeRepository initiativeRepository,
    IPersonRepository personRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<InitiativeResponse> HandleAsync(
        Guid initiativeId, LinkPersonRequest request, CancellationToken cancellationToken)
    {
        var initiative = await initiativeRepository.GetByIdAsync(initiativeId, cancellationToken)
            ?? throw new InvalidOperationException("Initiative not found.");

        if (initiative.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Initiative not found.");

        var person = await personRepository.GetByIdAsync(request.PersonId, cancellationToken);

        if (person is null || person.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Person not found.");

        initiative.LinkPerson(request.PersonId);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return InitiativeResponse.From(initiative);
    }
}
