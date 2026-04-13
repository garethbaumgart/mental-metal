using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Brief;

public sealed class LogInitiativeBriefDecisionHandler(
    IInitiativeRepository repo,
    ICurrentUserService currentUser,
    IUnitOfWork uow)
{
    public async Task<LivingBriefDto> HandleAsync(Guid initiativeId, LogDecisionRequest request, CancellationToken ct)
    {
        var initiative = (await repo.GetByIdAsync(initiativeId, ct)).EnsureOwned(currentUser.UserId, initiativeId);
        initiative.RecordDecision(request.Description, request.Rationale, BriefSource.Manual, []);
        await uow.SaveChangesAsync(ct);
        return LivingBriefDto.From(initiative);
    }
}
