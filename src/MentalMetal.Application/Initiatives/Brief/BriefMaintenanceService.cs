using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MentalMetal.Application.Initiatives.Brief;

public sealed class BriefMaintenanceService(
    IInitiativeRepository initiativeRepository,
    ICaptureRepository captureRepository,
    IPendingBriefUpdateRepository pendingBriefUpdateRepository,
    IUserRepository userRepository,
    IAiCompletionService aiCompletionService,
    IUnitOfWork unitOfWork,
    BriefRefreshQueue queue,
    ILogger<BriefMaintenanceService> logger) : IBriefMaintenanceService
{
    public void EnqueueRefresh(Guid userId, Guid initiativeId) =>
        queue.Enqueue(userId, initiativeId);

    public async Task<Guid> RefreshAsync(Guid userId, Guid initiativeId, CancellationToken cancellationToken)
    {
        var initiative = await initiativeRepository.GetByIdAsync(initiativeId, cancellationToken)
            ?? throw new InvalidOperationException($"Initiative '{initiativeId}' not found.");

        if (initiative.UserId != userId)
            throw new InvalidOperationException($"Initiative '{initiativeId}' not owned by user.");

        var briefVersion = initiative.Brief?.BriefVersion ?? 0;

        // Gather linked confirmed captures (we filter in-memory because cross-user-scoped reads
        // would require a separate user-scoped lookup; in production this would use a query).
        var allCaptures = await captureRepository.GetAllAsync(userId, typeFilter: null, statusFilter: null, cancellationToken);
        var linkedCaptures = allCaptures
            .Where(c => c.LinkedInitiativeIds.Contains(initiativeId))
            .Where(c => c.ExtractionStatus == ExtractionStatus.Confirmed && c.AiExtraction is not null)
            .OrderByDescending(c => c.CapturedAt)
            .Take(20)
            .ToList();

        try
        {
            var prompt = BuildPrompt(initiative, linkedCaptures);
            var aiResponse = await aiCompletionService.CompleteAsync(
                new AiCompletionRequest(SystemPrompt, prompt, Temperature: 0.3f),
                cancellationToken);

            var proposal = ParseProposal(aiResponse.Content, linkedCaptures.Select(c => c.Id).ToList());

            var pending = PendingBriefUpdate.Create(userId, initiativeId, proposal, briefVersion);
            await pendingBriefUpdateRepository.AddAsync(pending, cancellationToken);

            // Auto-apply gating
            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user?.Preferences.LivingBriefAutoApply == true)
            {
                ApplyProposalToInitiative(initiative, proposal);
                pending.MarkApplied();
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return pending.Id;
        }
        catch (TasteLimitExceededException)
        {
            return await PersistFailedAsync(userId, initiativeId, briefVersion,
                "Daily AI limit reached", cancellationToken);
        }
        catch (AiProviderException ex)
        {
            return await PersistFailedAsync(userId, initiativeId, briefVersion,
                $"AI provider error: {ex.Message}", cancellationToken);
        }
        catch (JsonException ex)
        {
            return await PersistFailedAsync(userId, initiativeId, briefVersion,
                $"Failed to parse AI response: {ex.Message}", cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unexpected error refreshing brief for {InitiativeId}", initiativeId);
            return await PersistFailedAsync(userId, initiativeId, briefVersion,
                $"Unexpected error: {ex.Message}", cancellationToken);
        }
    }

    private async Task<Guid> PersistFailedAsync(Guid userId, Guid initiativeId, int briefVersion, string reason, CancellationToken ct)
    {
        var failed = PendingBriefUpdate.CreateFailed(userId, initiativeId, briefVersion, reason);
        await pendingBriefUpdateRepository.AddAsync(failed, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return failed.Id;
    }

    /// <summary>
    /// Apply a proposal to an Initiative aggregate. Used by both auto-apply and the explicit Apply handler.
    /// </summary>
    public static void ApplyProposalToInitiative(Initiative initiative, BriefUpdateProposal proposal)
    {
        if (!string.IsNullOrWhiteSpace(proposal.ProposedSummary))
            initiative.RefreshSummary(proposal.ProposedSummary, BriefSource.AI, proposal.SourceCaptureIds);

        foreach (var d in proposal.NewDecisions)
            initiative.RecordDecision(d.Description, d.Rationale, BriefSource.AI,
                d.SourceCaptureIds.Count > 0 ? d.SourceCaptureIds : proposal.SourceCaptureIds);

        foreach (var r in proposal.NewRisks)
            initiative.RaiseRisk(r.Description, r.Severity, BriefSource.AI,
                r.SourceCaptureIds.Count > 0 ? r.SourceCaptureIds : proposal.SourceCaptureIds);

        foreach (var rid in proposal.RisksToResolve)
        {
            try { initiative.ResolveRisk(rid, "Resolved by AI proposal"); }
            catch (ArgumentException) { /* unknown risk id — skip */ }
            catch (InvalidOperationException) { /* already resolved — skip */ }
        }

        if (!string.IsNullOrWhiteSpace(proposal.ProposedRequirementsContent))
            initiative.SnapshotRequirements(proposal.ProposedRequirementsContent, BriefSource.AI, proposal.SourceCaptureIds);

        if (!string.IsNullOrWhiteSpace(proposal.ProposedDesignDirectionContent))
            initiative.SnapshotDesignDirection(proposal.ProposedDesignDirectionContent, BriefSource.AI, proposal.SourceCaptureIds);
    }

    private const string SystemPrompt = """
        You are an AI assistant that maintains a "Living Brief" for a project initiative.
        You are given the initiative's current brief and a list of linked captures (notes/transcripts).
        Produce a JSON object with this exact shape:

        {
          "proposedSummary": "Updated 1-3 paragraph executive summary, or null to leave unchanged",
          "newDecisions": [
            { "description": "Decision made", "rationale": "Why", "sourceCaptureIds": ["<guid>"] }
          ],
          "newRisks": [
            { "description": "Risk", "severity": "Low|Medium|High|Critical", "sourceCaptureIds": ["<guid>"] }
          ],
          "risksToResolve": ["<existing risk guid>"],
          "proposedRequirementsContent": "Full updated requirements text, or null if no change",
          "proposedDesignDirectionContent": "Full updated design direction text, or null if no change",
          "aiConfidence": 0.0-1.0,
          "rationale": "Brief explanation of what changed and why, citing capture IDs"
        }

        Rules:
        - Do NOT re-include decisions/risks already present in the current brief — list only NEW ones.
        - Use sourceCaptureIds to attribute each new item to the captures that justify it.
        - Use null (not empty string) to indicate no change for the optional fields.
        - Respond with ONLY the JSON object.
        """;

    private static string BuildPrompt(Initiative initiative, IReadOnlyList<Capture> linkedCaptures)
    {
        var brief = initiative.Brief ?? Domain.Initiatives.LivingBrief.LivingBrief.Empty();

        var input = new
        {
            initiativeTitle = initiative.Title,
            currentBrief = new
            {
                summary = brief.Summary,
                openRisks = brief.Risks.Where(r => r.Status == RiskStatus.Open).Select(r => new { r.Id, r.Description, severity = r.Severity.ToString() }),
                recentDecisions = brief.KeyDecisions.TakeLast(10).Select(d => new { d.Id, d.Description, d.Rationale }),
                latestRequirements = brief.RequirementsHistory.LastOrDefault()?.Content,
                latestDesignDirection = brief.DesignDirectionHistory.LastOrDefault()?.Content,
            },
            linkedCaptures = linkedCaptures.Select(c => new
            {
                captureId = c.Id,
                createdAt = c.CapturedAt,
                summary = c.AiExtraction?.Summary,
                decisions = c.AiExtraction?.Decisions,
                risks = c.AiExtraction?.RisksIdentified,
            })
        };
        return JsonSerializer.Serialize(input);
    }

    internal static BriefUpdateProposal ParseProposal(string jsonContent, IReadOnlyList<Guid> sourceCaptureIds)
    {
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        return new BriefUpdateProposal
        {
            ProposedSummary = ReadOptionalString(root, "proposedSummary"),
            NewDecisions = ReadArray(root, "newDecisions", e => new ProposedDecision
            {
                Description = e.GetProperty("description").GetString() ?? string.Empty,
                Rationale = ReadOptionalString(e, "rationale"),
                SourceCaptureIds = ReadGuidArray(e, "sourceCaptureIds"),
            }).Where(d => !string.IsNullOrWhiteSpace(d.Description)).ToList(),
            NewRisks = ReadArray(root, "newRisks", e => new ProposedRisk
            {
                Description = e.GetProperty("description").GetString() ?? string.Empty,
                Severity = ParseSeverity(ReadOptionalString(e, "severity")),
                SourceCaptureIds = ReadGuidArray(e, "sourceCaptureIds"),
            }).Where(r => !string.IsNullOrWhiteSpace(r.Description)).ToList(),
            RisksToResolve = ReadGuidArray(root, "risksToResolve"),
            ProposedRequirementsContent = ReadOptionalString(root, "proposedRequirementsContent"),
            ProposedDesignDirectionContent = ReadOptionalString(root, "proposedDesignDirectionContent"),
            SourceCaptureIds = sourceCaptureIds,
            AiConfidence = ReadOptionalDecimal(root, "aiConfidence"),
            Rationale = ReadOptionalString(root, "rationale"),
        };
    }

    private static List<T> ReadArray<T>(JsonElement root, string property, Func<JsonElement, T> map)
    {
        if (!root.TryGetProperty(property, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return [];
        return arr.EnumerateArray().Select(map).ToList();
    }

    private static List<Guid> ReadGuidArray(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return [];
        var result = new List<Guid>();
        foreach (var e in arr.EnumerateArray())
        {
            if (e.ValueKind == JsonValueKind.String && Guid.TryParse(e.GetString(), out var g))
                result.Add(g);
        }
        return result;
    }

    private static string? ReadOptionalString(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var p)) return null;
        if (p.ValueKind == JsonValueKind.Null) return null;
        var s = p.GetString();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static decimal? ReadOptionalDecimal(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var p)) return null;
        if (p.ValueKind == JsonValueKind.Number) return p.GetDecimal();
        return null;
    }

    private static RiskSeverity ParseSeverity(string? value) =>
        Enum.TryParse<RiskSeverity>(value, ignoreCase: true, out var s) ? s : RiskSeverity.Medium;
}

/// <summary>
/// Per-(user,initiative) debounced job queue. Concurrent enqueues for the same key
/// are coalesced — if a job is already in-flight or pending, additional triggers are dropped
/// because the in-flight job will pick up the latest state when it runs.
/// </summary>
public sealed class BriefRefreshQueue
{
    private readonly Channel<(Guid UserId, Guid InitiativeId)> _channel =
        Channel.CreateUnbounded<(Guid, Guid)>(new UnboundedChannelOptions { SingleReader = true });

    private readonly ConcurrentDictionary<(Guid, Guid), byte> _inFlight = new();

    public bool Enqueue(Guid userId, Guid initiativeId)
    {
        if (!_inFlight.TryAdd((userId, initiativeId), 0))
            return false; // coalesced
        return _channel.Writer.TryWrite((userId, initiativeId));
    }

    public ChannelReader<(Guid UserId, Guid InitiativeId)> Reader => _channel.Reader;

    public void MarkComplete(Guid userId, Guid initiativeId) =>
        _inFlight.TryRemove((userId, initiativeId), out _);

    public int InFlightCount => _inFlight.Count;
}

public sealed class BriefRefreshHostedService(
    BriefRefreshQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<BriefRefreshHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var (userId, initiativeId) in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IBriefMaintenanceService>();
                await service.RefreshAsync(userId, initiativeId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "BriefRefresh job failed for {InitiativeId}", initiativeId);
            }
            finally
            {
                queue.MarkComplete(userId, initiativeId);
            }
        }
    }
}
