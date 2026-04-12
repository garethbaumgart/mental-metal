using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.People;

public sealed class GetPeopleHandler(
    IPersonRepository personRepository,
    ICurrentUserService currentUserService)
{
    public async Task<List<PersonResponse>> HandleAsync(
        PersonType? typeFilter, bool includeArchived, CancellationToken cancellationToken)
    {
        var people = await personRepository.GetAllAsync(
            currentUserService.UserId, typeFilter, includeArchived, cancellationToken);

        return people.Select(PersonResponse.From).ToList();
    }
}
