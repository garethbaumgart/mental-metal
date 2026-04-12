using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.People;

public sealed class GetPersonHandler(
    IPersonRepository personRepository,
    ICurrentUserService currentUserService)
{
    public async Task<PersonResponse?> HandleAsync(
        Guid personId, CancellationToken cancellationToken)
    {
        var person = await personRepository.GetByIdAsync(personId, cancellationToken);

        if (person is null || person.UserId != currentUserService.UserId)
            return null;

        return PersonResponse.From(person);
    }
}
