using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives;

public sealed class RefreshSummaryHandler(
    IInitiativeRepository initiativeRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<InitiativeResponse> HandleAsync(
        Guid initiativeId, CancellationToken cancellationToken)
    {
        var initiative = await initiativeRepository.GetByIdAsync(initiativeId, cancellationToken)
            ?? throw new NotFoundException("Initiative", initiativeId);

        if (initiative.UserId != currentUserService.UserId)
            throw new NotFoundException("Initiative", initiativeId);

        // For now, the actual AI summary generation is Phase D work.
        // This endpoint exists so the API shape is correct; it will be
        // wired to the AI provider when the extraction pipeline is built.
        // For now, set a placeholder to confirm the domain method works.
        initiative.RefreshAutoSummary("Summary refresh requested. AI generation pending.");

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return InitiativeResponse.From(initiative);
    }
}
