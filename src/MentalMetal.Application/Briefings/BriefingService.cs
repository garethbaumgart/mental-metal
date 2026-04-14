using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Briefings;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.Options;

namespace MentalMetal.Application.Briefings;

public sealed record BriefingResult(Briefing Briefing, bool WasCached);

/// <summary>
/// Orchestrates briefing generation: assemble facts, decide cache vs generate,
/// call the AI provider, persist, return. Caller decides 200 vs 201 from
/// <see cref="BriefingResult.WasCached"/>.
/// </summary>
public sealed class BriefingService(
    BriefingFactsAssembler factsAssembler,
    BriefingPromptBuilder promptBuilder,
    IBriefingRepository briefingRepository,
    IAiCompletionService aiCompletionService,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IOptions<BriefingOptions> options,
    TimeProvider timeProvider)
{
    public async Task<BriefingResult> GenerateMorningAsync(bool force, CancellationToken cancellationToken)
    {
        var opts = options.Value;
        var facts = await factsAssembler.BuildMorningAsync(cancellationToken);
        var scopeKey = $"morning:{facts.UserLocalDate}";
        return await GenerateInternalAsync(
            BriefingType.Morning,
            scopeKey,
            facts,
            promptBuilder.BuildMorning(facts, opts.MaxBriefingTokens),
            opts.MorningBriefingStaleHours,
            force,
            cancellationToken);
    }

    public async Task<BriefingResult> GenerateWeeklyAsync(bool force, CancellationToken cancellationToken)
    {
        var opts = options.Value;
        var facts = await factsAssembler.BuildWeeklyAsync(cancellationToken);
        var scopeKey = $"weekly:{facts.IsoYear:D4}-W{facts.WeekNumber:D2}";
        return await GenerateInternalAsync(
            BriefingType.Weekly,
            scopeKey,
            facts,
            promptBuilder.BuildWeekly(facts, opts.MaxBriefingTokens),
            opts.WeeklyBriefingStaleHours,
            force,
            cancellationToken);
    }

    public async Task<BriefingResult?> GenerateOneOnOnePrepAsync(Guid personId, bool force, CancellationToken cancellationToken)
    {
        var opts = options.Value;
        var facts = await factsAssembler.BuildOneOnOnePrepAsync(personId, cancellationToken);
        if (facts is null)
            return null; // unknown or foreign person

        var scopeKey = $"oneonone:{personId:N}";
        return await GenerateInternalAsync(
            BriefingType.OneOnOnePrep,
            scopeKey,
            facts,
            promptBuilder.BuildOneOnOnePrep(facts, opts.MaxBriefingTokens),
            opts.OneOnOnePrepStaleHours,
            force,
            cancellationToken);
    }

    private async Task<BriefingResult> GenerateInternalAsync(
        BriefingType type,
        string scopeKey,
        object facts,
        AiCompletionRequest request,
        int staleHours,
        bool force,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var now = timeProvider.GetUtcNow();

        if (!force)
        {
            var cached = await briefingRepository.GetLatestAsync(userId, type, scopeKey, cancellationToken);
            if (cached is not null && (now - cached.GeneratedAtUtc) <= TimeSpan.FromHours(staleHours))
                return new BriefingResult(cached, WasCached: true);
        }

        AiCompletionResult ai;
        try
        {
            ai = await aiCompletionService.CompleteAsync(request, cancellationToken);
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("AI provider is not configured", StringComparison.OrdinalIgnoreCase))
        {
            throw new AiProviderNotConfiguredException(ex.Message);
        }

        var briefing = Briefing.Create(
            userId,
            type,
            scopeKey,
            now,
            ai.Content,
            promptBuilder.SerializeFacts(facts),
            ai.Model,
            ai.InputTokens,
            ai.OutputTokens);

        await briefingRepository.AddAsync(briefing, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new BriefingResult(briefing, WasCached: false);
    }
}
