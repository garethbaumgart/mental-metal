using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives;

public sealed class UpdateInitiativeTitleHandler(
    IInitiativeRepository initiativeRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<InitiativeResponse> HandleAsync(
        Guid initiativeId, UpdateTitleRequest request, CancellationToken cancellationToken)
    {
        var initiative = await initiativeRepository.GetByIdAsync(initiativeId, cancellationToken)
            ?? throw new NotFoundException("Initiative", initiativeId);

        if (initiative.UserId != currentUserService.UserId)
            throw new NotFoundException("Initiative", initiativeId);

        initiative.UpdateTitle(request.Title);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return InitiativeResponse.From(initiative);
    }
}
