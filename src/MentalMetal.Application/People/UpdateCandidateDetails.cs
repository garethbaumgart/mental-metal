using MentalMetal.Application.Common;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.People;

public sealed class UpdateCandidateDetailsHandler(
    IPersonRepository personRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<PersonResponse> HandleAsync(
        Guid personId, CandidateDetailsRequest request, CancellationToken cancellationToken)
    {
        var person = await personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Person not found.");

        if (person.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Person not found.");

        person.UpdateCandidateDetails(request.CvNotes, request.SourceChannel);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PersonResponse.From(person);
    }
}
