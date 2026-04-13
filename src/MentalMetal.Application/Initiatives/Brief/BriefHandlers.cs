using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Brief;

internal static class BriefOwnership
{
    public static Initiative EnsureOwned(this Initiative? initiative, Guid userId, Guid initiativeId)
    {
        if (initiative is null || initiative.UserId != userId)
            throw new NotFoundException("Initiative", initiativeId);
        return initiative;
    }

    public static PendingBriefUpdate EnsureOwned(this PendingBriefUpdate? update, Guid userId, Guid updateId)
    {
        if (update is null || update.UserId != userId)
            throw new NotFoundException("PendingBriefUpdate", updateId);
        return update;
    }
}

public sealed class GetInitiativeBriefHandler(
    IInitiativeRepository repo,
    ICurrentUserService currentUser)
{
    public async Task<LivingBriefDto?> HandleAsync(Guid initiativeId, CancellationToken ct)
    {
        var initiative = await repo.GetByIdAsync(initiativeId, ct);
        if (initiative is null || initiative.UserId != currentUser.UserId) return null;
        return LivingBriefDto.From(initiative);
    }
}

public sealed class UpdateInitiativeBriefSummaryHandler(
    IInitiativeRepository repo,
    ICurrentUserService currentUser,
    IUnitOfWork uow)
{
    public async Task<LivingBriefDto> HandleAsync(Guid initiativeId, UpdateSummaryRequest request, CancellationToken ct)
    {
        var initiative = (await repo.GetByIdAsync(initiativeId, ct)).EnsureOwned(currentUser.UserId, initiativeId);
        initiative.RefreshSummary(request.Summary ?? string.Empty, BriefSource.Manual, []);
        await uow.SaveChangesAsync(ct);
        return LivingBriefDto.From(initiative);
    }
}

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

public sealed class ResolveInitiativeBriefRiskHandler(
    IInitiativeRepository repo,
    ICurrentUserService currentUser,
    IUnitOfWork uow)
{
    public async Task<LivingBriefDto> HandleAsync(Guid initiativeId, Guid riskId, ResolveRiskRequest request, CancellationToken ct)
    {
        var initiative = (await repo.GetByIdAsync(initiativeId, ct)).EnsureOwned(currentUser.UserId, initiativeId);
        initiative.ResolveRisk(riskId, request?.ResolutionNote);
        await uow.SaveChangesAsync(ct);
        return LivingBriefDto.From(initiative);
    }
}

public sealed class SnapshotInitiativeBriefRequirementsHandler(
    IInitiativeRepository repo,
    ICurrentUserService currentUser,
    IUnitOfWork uow)
{
    public async Task<LivingBriefDto> HandleAsync(Guid initiativeId, SnapshotRequest request, CancellationToken ct)
    {
        var initiative = (await repo.GetByIdAsync(initiativeId, ct)).EnsureOwned(currentUser.UserId, initiativeId);
        initiative.SnapshotRequirements(request.Content, BriefSource.Manual, []);
        await uow.SaveChangesAsync(ct);
        return LivingBriefDto.From(initiative);
    }
}

public sealed class SnapshotInitiativeBriefDesignDirectionHandler(
    IInitiativeRepository repo,
    ICurrentUserService currentUser,
    IUnitOfWork uow)
{
    public async Task<LivingBriefDto> HandleAsync(Guid initiativeId, SnapshotRequest request, CancellationToken ct)
    {
        var initiative = (await repo.GetByIdAsync(initiativeId, ct)).EnsureOwned(currentUser.UserId, initiativeId);
        initiative.SnapshotDesignDirection(request.Content, BriefSource.Manual, []);
        await uow.SaveChangesAsync(ct);
        return LivingBriefDto.From(initiative);
    }
}

public sealed class RefreshInitiativeBriefHandler(
    IInitiativeRepository repo,
    ICurrentUserService currentUser,
    IBriefMaintenanceService brief)
{
    public async Task<Guid> HandleAsync(Guid initiativeId, CancellationToken ct)
    {
        var initiative = (await repo.GetByIdAsync(initiativeId, ct)).EnsureOwned(currentUser.UserId, initiativeId);
        return await brief.RefreshAsync(currentUser.UserId, initiative.Id, ct);
    }
}

public sealed class ListPendingBriefUpdatesHandler(
    IInitiativeRepository initiativeRepo,
    IPendingBriefUpdateRepository repo,
    ICurrentUserService currentUser)
{
    public async Task<IReadOnlyList<PendingBriefUpdateDto>?> HandleAsync(
        Guid initiativeId, PendingBriefUpdateStatus? statusFilter, CancellationToken ct)
    {
        var initiative = await initiativeRepo.GetByIdAsync(initiativeId, ct);
        if (initiative is null || initiative.UserId != currentUser.UserId) return null;

        var current = initiative.Brief?.BriefVersion ?? 0;
        var list = await repo.ListForInitiativeAsync(currentUser.UserId, initiativeId, statusFilter, ct);
        return list.Select(p => PendingBriefUpdateDto.From(p, current)).ToList();
    }
}

public sealed class GetPendingBriefUpdateHandler(
    IInitiativeRepository initiativeRepo,
    IPendingBriefUpdateRepository repo,
    ICurrentUserService currentUser)
{
    public async Task<PendingBriefUpdateDto?> HandleAsync(Guid updateId, CancellationToken ct)
    {
        var p = await repo.GetByIdAsync(updateId, ct);
        if (p is null || p.UserId != currentUser.UserId) return null;
        var initiative = await initiativeRepo.GetByIdAsync(p.InitiativeId, ct);
        var current = initiative?.Brief?.BriefVersion ?? p.BriefVersionAtProposal;
        return PendingBriefUpdateDto.From(p, current);
    }
}

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

public sealed class RejectPendingBriefUpdateHandler(
    IPendingBriefUpdateRepository repo,
    ICurrentUserService currentUser,
    IUnitOfWork uow)
{
    public async Task HandleAsync(Guid initiativeId, Guid updateId, RejectPendingUpdateRequest? request, CancellationToken ct)
    {
        var pending = (await repo.GetByIdAsync(updateId, ct)).EnsureOwned(currentUser.UserId, updateId);
        if (pending.InitiativeId != initiativeId)
            throw new NotFoundException("PendingBriefUpdate", updateId);
        pending.Reject(request?.Reason);
        await uow.SaveChangesAsync(ct);
    }
}

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

        var newProposal = new BriefUpdateProposal
        {
            ProposedSummary = string.IsNullOrWhiteSpace(request.ProposedSummary) ? pending.Proposal.ProposedSummary : request.ProposedSummary,
            NewDecisions = (request.NewDecisions ?? pending.Proposal.NewDecisions.Select(d => new ProposedDecisionDto(d.Description, d.Rationale, d.SourceCaptureIds)).ToList())
                .Select(d => new ProposedDecision { Description = d.Description, Rationale = d.Rationale, SourceCaptureIds = d.SourceCaptureIds }).ToList(),
            NewRisks = (request.NewRisks ?? pending.Proposal.NewRisks.Select(r => new ProposedRiskDto(r.Description, r.Severity.ToString(), r.SourceCaptureIds)).ToList())
                .Select(r => new ProposedRisk
                {
                    Description = r.Description,
                    Severity = Enum.TryParse<RiskSeverity>(r.Severity, true, out var s) ? s : RiskSeverity.Medium,
                    SourceCaptureIds = r.SourceCaptureIds
                }).ToList(),
            RisksToResolve = request.RisksToResolve ?? pending.Proposal.RisksToResolve.ToList(),
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
