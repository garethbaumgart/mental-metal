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
    ICurrentUserService currentUserService)
{
    public async Task<DailyBriefResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterdayStart = today.AddDays(-1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var yesterdayEnd = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Get all processed captures and filter to yesterday
        var allCaptures = await captureRepository.GetAllAsync(
            userId, null, ProcessingStatus.Processed, cancellationToken);

        var yesterdayCaptures = allCaptures
            .Where(c => c.CapturedAt >= new DateTimeOffset(yesterdayStart)
                     && c.CapturedAt < new DateTimeOffset(yesterdayEnd))
            .OrderByDescending(c => c.CapturedAt)
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
            c.AiExtraction?.Decisions.ToList() ?? [],
            c.AiExtraction?.Risks.ToList() ?? [])).ToList();

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
