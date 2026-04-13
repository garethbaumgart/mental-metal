using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Brief;

public sealed class ApplyPendingBriefUpdateHandler(
    IInitiativeRepository initiativeRepo,
    IPendingBriefUpdateRepository repo,
    ICurrentUserService currentUser,
    IUnitOfWork uow)
{
    public sealed class StaleProposalException : Exception
    {
        public int CurrentBriefVersion { get; }
        public int ProposalBriefVersion { get; }
        public StaleProposalException(int current, int proposal)
            : base($"Pending update is stale: brief is at version {current}, proposal targeted version {proposal}.")
        {
            CurrentBriefVersion = current;
            ProposalBriefVersion = proposal;
        }
    }

    public async Task<LivingBriefDto> HandleAsync(Guid initiativeId, Guid updateId, CancellationToken ct)
    {
        var pending = (await repo.GetByIdAsync(updateId, ct)).EnsureOwned(currentUser.UserId, updateId);
        if (pending.InitiativeId != initiativeId)
            throw new NotFoundException("PendingBriefUpdate", updateId);

        var initiative = await initiativeRepo.GetByIdAsync(pending.InitiativeId, ct)
            ?? throw new NotFoundException("Initiative", pending.InitiativeId);
        if (initiative.UserId != currentUser.UserId)
            throw new NotFoundException("Initiative", pending.InitiativeId);

        var currentVersion = initiative.Brief?.BriefVersion ?? 0;
        if (currentVersion > pending.BriefVersionAtProposal)
            throw new StaleProposalException(currentVersion, pending.BriefVersionAtProposal);

        BriefMaintenanceService.ApplyProposalToInitiative(initiative, pending.Proposal);
        pending.MarkApplied();

        await uow.SaveChangesAsync(ct);
        return LivingBriefDto.From(initiative);
    }
}
