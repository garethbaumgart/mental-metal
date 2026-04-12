using MentalMetal.Application.Common;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives;

public sealed class CreateInitiativeHandler(
    IInitiativeRepository initiativeRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<InitiativeResponse> HandleAsync(
        CreateInitiativeRequest request, CancellationToken cancellationToken)
    {
        var initiative = Initiative.Create(currentUserService.UserId, request.Title);

        await initiativeRepository.AddAsync(initiative, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return InitiativeResponse.From(initiative);
    }
}
