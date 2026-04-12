using MentalMetal.Application.Common;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.People;

public sealed class AdvanceCandidatePipelineHandler(
    IPersonRepository personRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<PersonResponse> HandleAsync(
        Guid personId, AdvancePipelineRequest request, CancellationToken cancellationToken)
    {
        var person = await personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Person not found.");

        if (person.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Person not found.");

        person.AdvanceCandidatePipeline(request.NewStatus);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PersonResponse.From(person);
    }
}
