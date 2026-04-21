using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Briefings;

public sealed class GenerateWeeklyBriefHandler(
    ICaptureRepository captureRepository,
    ICommitmentRepository commitmentRepository,
    IInitiativeRepository initiativeRepository,
    IAiCompletionService aiCompletionService,
    ICurrentUserService currentUserService)
{
    public const string NoDataNarrative =
        "No captures or commitment activity were recorded this week.";
    public async Task<WeeklyBriefResponse> HandleAsync(
        DateOnly? weekOf,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        // Default to the current week's Monday
        var referenceDate = weekOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var dayOfWeek = referenceDate.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)referenceDate.DayOfWeek - 1;
        var weekStart = referenceDate.AddDays(-dayOfWeek);
        var weekEnd = weekStart.AddDays(7);

        var weekStartOffset = new DateTimeOffset(weekStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var weekEndOffset = new DateTimeOffset(weekEnd.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        // TODO: ICaptureRepository.GetAllAsync does not accept a date range — all captures are
        // loaded then filtered in memory. Add a date-range overload to push filtering to the
        // database once capture volume warrants it.
        var allCaptures = await captureRepository.GetAllAsync(
            userId, null, ProcessingStatus.Processed, cancellationToken);

        var weekCaptures = allCaptures
            .Where(c => c.CapturedAt >= weekStartOffset && c.CapturedAt < weekEndOffset)
            .OrderByDescending(c => c.CapturedAt)
            .ToList();

        // TODO: Same optimisation opportunity — ICommitmentRepository.GetAllAsync does not
        // accept a date range, so we load all commitments and filter in memory.
        var allCommitments = await commitmentRepository.GetAllAsync(
            userId, null, null, null, null, null, cancellationToken);

        var newThisWeek = allCommitments
            .Count(c => c.CreatedAt >= weekStartOffset && c.CreatedAt < weekEndOffset);

        var completedThisWeek = allCommitments
            .Count(c => c.Status == CommitmentStatus.Completed
                     && c.CompletedAt.HasValue
                     && c.CompletedAt.Value >= weekStartOffset
                     && c.CompletedAt.Value < weekEndOffset);

        var overdueCount = allCommitments
            .Count(c => c.IsOverdue);

        var totalOpen = allCommitments
            .Count(c => c.Status == CommitmentStatus.Open);

        // Get initiatives with linked captures from this period
        var initiatives = await initiativeRepository.GetAllAsync(
            userId, InitiativeStatus.Active, cancellationToken);

        var initiativeActivity = new List<InitiativeActivityDto>();
        foreach (var initiative in initiatives)
        {
            var linkedCaptureCount = weekCaptures
                .Count(c => c.LinkedInitiativeIds.Contains(initiative.Id));

            var linkedCommitmentCount = allCommitments
                .Count(c => c.InitiativeId == initiative.Id
                         && c.CreatedAt >= weekStartOffset
                         && c.CreatedAt < weekEndOffset);

            if (linkedCaptureCount > 0 || linkedCommitmentCount > 0)
            {
                // DTO intentionally reports capture count only — commitment count is used
                // solely as an inclusion criterion so initiatives with commitment-only activity
                // appear in the brief rather than being silently excluded.
                initiativeActivity.Add(new InitiativeActivityDto(
                    initiative.Id,
                    initiative.Title,
                    linkedCaptureCount,
                    initiative.AutoSummary));
            }
        }

        // Build AI prompt — send summaries, not raw transcripts, to manage token budget
        var captureContexts = weekCaptures.Select(c => new CaptureContextForBrief(
            c.Title,
            c.CapturedAt,
            c.AiExtraction?.Summary,
            c.AiExtraction?.Decisions?.ToList() ?? [],
            c.AiExtraction?.Risks?.ToList() ?? [])).ToList();

        var initiativeContexts = initiativeActivity.Select(i => new InitiativeContextForBrief(
            i.Title,
            i.CaptureCount,
            i.AutoSummary)).ToList();

        // When there are no captures and no commitment activity, skip AI and return a clean
        // "no data" response to avoid generating misleading narrative from empty context.
        if (weekCaptures.Count == 0 && newThisWeek == 0 && completedThisWeek == 0 && overdueCount == 0)
        {
            return new WeeklyBriefResponse(
                NoDataNarrative,
                [],
                [],
                new CommitmentStatusSummary(newThisWeek, completedThisWeek, overdueCount, totalOpen),
                [],
                initiativeActivity,
                new DateRange(weekStartOffset, weekEndOffset),
                DateTimeOffset.UtcNow);
        }

        var userPrompt = WeeklyBriefPromptBuilder.BuildUserPrompt(
            captureContexts,
            newThisWeek,
            completedThisWeek,
            overdueCount,
            initiativeContexts,
            weekStartOffset,
            weekEndOffset);

        // Call AI
        var aiRequest = new AiCompletionRequest(
            WeeklyBriefPromptBuilder.SystemPrompt,
            userPrompt,
            MaxTokens: 4096,
            Temperature: 0.3f);

        var aiResult = await aiCompletionService.CompleteAsync(aiRequest, cancellationToken);

        // Collect all decisions and risks from the week's captures
        var allDecisions = weekCaptures
            .Where(c => c.AiExtraction is not null)
            .SelectMany(c => c.AiExtraction!.Decisions ?? [])
            .Distinct()
            .ToList();

        var allRisks = weekCaptures
            .Where(c => c.AiExtraction is not null)
            .SelectMany(c => c.AiExtraction!.Risks ?? [])
            .Distinct()
            .ToList();

        // Extract cross-conversation insights from the AI narrative
        // The AI produces these inline; we also collect shared person mentions across captures
        var crossInsights = FindCrossConversationPatterns(weekCaptures);

        return new WeeklyBriefResponse(
            aiResult.Content,
            crossInsights,
            allDecisions,
            new CommitmentStatusSummary(newThisWeek, completedThisWeek, overdueCount, totalOpen),
            allRisks,
            initiativeActivity,
            new DateRange(weekStartOffset, weekEndOffset),
            DateTimeOffset.UtcNow);
    }

    private static List<string> FindCrossConversationPatterns(List<Capture> captures)
    {
        if (captures.Count < 2)
            return [];

        // Find people mentioned in multiple captures
        var personCaptureCount = new Dictionary<Guid, int>();
        foreach (var capture in captures)
        {
            foreach (var pid in capture.LinkedPersonIds.Distinct())
            {
                personCaptureCount.TryGetValue(pid, out var count);
                personCaptureCount[pid] = count + 1;
            }
        }

        var insights = new List<string>();

        var multiCapturePeople = personCaptureCount
            .Where(kv => kv.Value >= 2)
            .OrderByDescending(kv => kv.Value)
            .Take(3);

        foreach (var kv in multiCapturePeople)
        {
            // Find the person's name from extraction data — skip if unresolvable
            var name = captures
                .SelectMany(c => c.AiExtraction?.PeopleMentioned ?? [])
                .FirstOrDefault(p => p.PersonId == kv.Key)?.RawName;

            if (!string.IsNullOrWhiteSpace(name))
                insights.Add($"{name} appeared in {kv.Value} conversations this week");
        }

        return insights;
    }
}
