using MentalMetal.Application.Common;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.People;

public sealed class UpdateCareerDetailsHandler(
    IPersonRepository personRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<PersonResponse> HandleAsync(
        Guid personId, CareerDetailsRequest request, CancellationToken cancellationToken)
    {
        var person = await personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Person not found.");

        if (person.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Person not found.");

        if (person.IsArchived)
            throw new InvalidOperationException("Cannot modify an archived person.");

        person.UpdateCareerDetails(request.Level, request.Aspirations, request.GrowthAreas);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PersonResponse.From(person);
    }
}
