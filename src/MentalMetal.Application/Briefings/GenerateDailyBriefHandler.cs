using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Briefings;

public sealed class GenerateDailyBriefHandler(
    ICaptureRepository captureRepository,
    ICommitmentRepository commitmentRepository,
    IPersonRepository personRepository,
    IAiCompletionService aiCompletionService,
    ICurrentUserService currentUserService,
    IBriefCacheService briefCacheService)
{
    public async Task<DailyBriefResponse> HandleAsync(
        bool forceRefresh, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        if (!forceRefresh)
        {
            var cached = briefCacheService.GetDailyBrief(userId);
            if (cached is not null)
                return cached;
        }

        var response = await GenerateAsync(userId, cancellationToken);
        briefCacheService.SetDailyBrief(userId, response);
        return response;
    }

    private async Task<DailyBriefResponse> GenerateAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterdayStart = new DateTimeOffset(
            today.AddDays(-1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var yesterdayEnd = new DateTimeOffset(
            today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        // Use date-range query instead of loading all captures
        var yesterdayCaptures = (await captureRepository.GetByDateRangeAsync(
            userId, yesterdayStart, yesterdayEnd, ProcessingStatus.Processed, cancellationToken))
            .ToList();

        // Get commitments
        var allOpenCommitments = await commitmentRepository.GetAllAsync(
            userId, null, CommitmentStatus.Open, null, null, null, cancellationToken);

        var dueToday = allOpenCommitments
            .Where(c => c.DueDate == today)
            .ToList();

        var overdue = allOpenCommitments
            .Where(c => c.IsOverdue)
            .ToList();

        // Fresh commitments = spawned from yesterday's captures
        var yesterdayCaptureIds = yesterdayCaptures.Select(c => c.Id).ToHashSet();
        var freshCommitments = allOpenCommitments
            .Where(c => c.SourceCaptureId.HasValue && yesterdayCaptureIds.Contains(c.SourceCaptureId.Value))
            .ToList();

        // Get people for name resolution — include IDs from both commitments and capture mentions
        var capturePersonIds = yesterdayCaptures.SelectMany(c => c.LinkedPersonIds);
        var commitmentPersonIds = allOpenCommitments.Select(c => c.PersonId);
        var allPersonIds = commitmentPersonIds.Concat(capturePersonIds).Distinct().ToList();
        var people = await personRepository.GetByIdsAsync(userId, allPersonIds, cancellationToken);
        var personLookup = people.ToDictionary(p => p.Id, p => p.Name);

        // Build people activity (top 5 mentioned yesterday)
        var personMentionCounts = new Dictionary<Guid, int>();
        foreach (var capture in yesterdayCaptures)
        {
            foreach (var pid in capture.LinkedPersonIds)
            {
                personMentionCounts.TryGetValue(pid, out var count);
                personMentionCounts[pid] = count + 1;
            }
        }

        var topPeople = personMentionCounts
            .OrderByDescending(kv => kv.Value)
            .Take(5)
            .Select(kv => new PersonActivityDto(
                kv.Key,
                personLookup.GetValueOrDefault(kv.Key, "Unknown"),
                kv.Value))
            .ToList();

        // Build AI prompt
        var captureContexts = yesterdayCaptures.Select(c => new CaptureContextForBrief(
            c.Title,
            c.CapturedAt,
            c.AiExtraction?.Summary,
            c.AiExtraction?.Decisions?.ToList() ?? [],
            c.AiExtraction?.Risks?.ToList() ?? [])).ToList();

        var dueTodayContexts = dueToday.Select(c => new CommitmentContextForBrief(
            c.Description,
            c.Direction.ToString(),
            c.DueDate,
            personLookup.GetValueOrDefault(c.PersonId))).ToList();

        var overdueContexts = overdue.Select(c => new CommitmentContextForBrief(
            c.Description,
            c.Direction.ToString(),
            c.DueDate,
            personLookup.GetValueOrDefault(c.PersonId))).ToList();

        var userPrompt = DailyBriefPromptBuilder.BuildUserPrompt(
            captureContexts, dueTodayContexts, overdueContexts);

        // Call AI
        var aiRequest = new AiCompletionRequest(
            DailyBriefPromptBuilder.SystemPrompt,
            userPrompt,
            MaxTokens: 2048,
            Temperature: 0.3f);

        var aiResult = await aiCompletionService.CompleteAsync(aiRequest, cancellationToken);

        // Build response
        BriefCommitmentDto ToDto(Commitment c) => new(
            c.Id,
            c.Description,
            c.Direction,
            c.PersonId,
            personLookup.GetValueOrDefault(c.PersonId),
            c.DueDate,
            c.IsOverdue,
            c.Confidence);

        return new DailyBriefResponse(
            aiResult.Content,
            freshCommitments.Select(ToDto).ToList(),
            dueToday.Select(ToDto).ToList(),
            overdue.Select(ToDto).ToList(),
            topPeople,
            yesterdayCaptures.Count,
            DateTimeOffset.UtcNow);
    }
}
