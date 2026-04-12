using MentalMetal.Application.Common;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.People;

public sealed class UpdatePersonProfileHandler(
    IPersonRepository personRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<PersonResponse> HandleAsync(
        Guid personId, UpdatePersonRequest request, CancellationToken cancellationToken)
    {
        var person = await personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Person not found.");

        if (person.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Person not found.");

        if (person.IsArchived)
            throw new InvalidOperationException("Cannot update an archived person.");

        if (await personRepository.ExistsByNameAsync(
                currentUserService.UserId, request.Name, excludeId: personId, cancellationToken))
            throw new InvalidOperationException(
                $"A person with name '{request.Name}' already exists.");

        person.UpdateProfile(request.Name, request.Email, request.Role, request.Team, request.Notes);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PersonResponse.From(person);
    }
}
