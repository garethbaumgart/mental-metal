using MentalMetal.Application.Common;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.People;

public sealed class CreatePersonHandler(
    IPersonRepository personRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<PersonResponse> HandleAsync(
        CreatePersonRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        if (await personRepository.ExistsByNameAsync(userId, request.Name, excludeId: null, cancellationToken))
            throw new InvalidOperationException(
                $"A person with name '{request.Name}' already exists.");

        var person = Person.Create(userId, request.Name, request.Type, request.Email, request.Role);

        if (request.Team is not null)
            person.UpdateProfile(request.Name, request.Email, request.Role, request.Team, notes: null);

        await personRepository.AddAsync(person, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PersonResponse.From(person);
    }
}
