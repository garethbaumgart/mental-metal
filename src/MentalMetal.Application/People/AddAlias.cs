using MentalMetal.Application.Common;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.People;

public sealed class AddAliasHandler(
    IPersonRepository personRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<PersonResponse> HandleAsync(
        Guid personId, AddAliasRequest request, CancellationToken cancellationToken)
    {
        var person = await personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Person not found.");

        if (person.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Person not found.");

        if (person.IsArchived)
            throw new InvalidOperationException("Cannot modify an archived person.");

        person.AddAlias(request.Alias);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PersonResponse.From(person);
    }
}
