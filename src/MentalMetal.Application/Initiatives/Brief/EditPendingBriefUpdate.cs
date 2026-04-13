using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Brief;

public sealed class EditPendingBriefUpdateHandler(
    IInitiativeRepository initiativeRepo,
    IPendingBriefUpdateRepository repo,
    ICurrentUserService currentUser,
    IUnitOfWork uow)
{
    public async Task<PendingBriefUpdateDto> HandleAsync(Guid initiativeId, Guid updateId, EditPendingUpdateRequest request, CancellationToken ct)
    {
        var pending = (await repo.GetByIdAsync(updateId, ct)).EnsureOwned(currentUser.UserId, updateId);
        if (pending.InitiativeId != initiativeId)
            throw new NotFoundException("PendingBriefUpdate", updateId);

        var newRisks = pending.Proposal.NewRisks;
        if (request.NewRisks is not null)
        {
            var mapped = new List<ProposedRisk>(request.NewRisks.Count);
            foreach (var r in request.NewRisks)
            {
                // Reject invalid severities instead of silently coercing to Medium —
                // matches the strict validation in RaiseInitiativeBriefRiskHandler.
                if (!Enum.TryParse<RiskSeverity>(r.Severity, ignoreCase: true, out var severity))
                    throw new ArgumentException($"'{r.Severity}' is not a valid risk severity.");
                mapped.Add(new ProposedRisk
                {
                    Description = r.Description,
                    Severity = severity,
                    SourceCaptureIds = [.. r.SourceCaptureIds],
                });
            }
            newRisks = [.. mapped];
        }

        var newProposal = new BriefUpdateProposal
        {
            ProposedSummary = string.IsNullOrWhiteSpace(request.ProposedSummary) ? pending.Proposal.ProposedSummary : request.ProposedSummary,
            NewDecisions = request.NewDecisions is not null
                ? [.. request.NewDecisions.Select(d => new ProposedDecision
                {
                    Description = d.Description,
                    Rationale = d.Rationale,
                    SourceCaptureIds = [.. d.SourceCaptureIds]
                })]
                : pending.Proposal.NewDecisions,
            NewRisks = newRisks,
            RisksToResolve = request.RisksToResolve is not null ? [.. request.RisksToResolve] : pending.Proposal.RisksToResolve,
            ProposedRequirementsContent = request.ProposedRequirementsContent ?? pending.Proposal.ProposedRequirementsContent,
            ProposedDesignDirectionContent = request.ProposedDesignDirectionContent ?? pending.Proposal.ProposedDesignDirectionContent,
            SourceCaptureIds = pending.Proposal.SourceCaptureIds,
            AiConfidence = pending.Proposal.AiConfidence,
            Rationale = request.Rationale ?? pending.Proposal.Rationale,
        };

        pending.Edit(newProposal);
        await uow.SaveChangesAsync(ct);

        // Use the initiative's CURRENT brief version so CurrentInitiativeBriefVersion / IsStale are accurate.
        var initiative = await initiativeRepo.GetByIdAsync(pending.InitiativeId, ct);
        var currentVersion = initiative?.Brief?.BriefVersion ?? pending.BriefVersionAtProposal;
        return PendingBriefUpdateDto.From(pending, currentVersion);
    }
}
