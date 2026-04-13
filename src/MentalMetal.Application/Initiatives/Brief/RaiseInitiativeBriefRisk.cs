using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Brief;

public sealed class RaiseInitiativeBriefRiskHandler(
    IInitiativeRepository repo,
    ICurrentUserService currentUser,
    IUnitOfWork uow)
{
    public async Task<LivingBriefDto> HandleAsync(Guid initiativeId, RaiseRiskRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<RiskSeverity>(request.Severity, ignoreCase: true, out var severity))
            throw new ArgumentException($"'{request.Severity}' is not a valid risk severity.");
        var initiative = (await repo.GetByIdAsync(initiativeId, ct)).EnsureOwned(currentUser.UserId, initiativeId);
        initiative.RaiseRisk(request.Description, severity, BriefSource.Manual, []);
        await uow.SaveChangesAsync(ct);
        return LivingBriefDto.From(initiative);
    }
}
