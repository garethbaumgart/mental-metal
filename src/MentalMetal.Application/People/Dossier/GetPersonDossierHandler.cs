using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.People.Dossier;

public sealed class GetPersonDossierHandler(
    IPersonRepository personRepository,
    ICaptureRepository captureRepository,
    ICommitmentRepository commitmentRepository,
    IAiCompletionService aiCompletionService,
    ICurrentUserService currentUserService)
{
    public async Task<PersonDossierResponse> HandleAsync(
        Guid personId,
        string mode,
        int captureLimit,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var person = await personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new NotFoundException("Person", personId);

        if (person.UserId != userId)
            throw new NotFoundException("Person", personId);

        // Get all captures for this user, then filter to those linked to this person
        var allCaptures = await captureRepository.GetAllAsync(
            userId, null, ProcessingStatus.Processed, cancellationToken);

        var linkedCaptures = allCaptures
            .Where(c => c.LinkedPersonIds.Contains(personId))
            .OrderByDescending(c => c.CapturedAt)
            .Take(captureLimit)
            .ToList();

        // Get open commitments for this person (both directions)
        var commitments = await commitmentRepository.GetAllAsync(
            userId, null, CommitmentStatus.Open, personId, null, null, cancellationToken);

        // Build transcript mentions
        var mentions = new List<TranscriptMentionDto>();
        var unresolvedMentions = new List<UnresolvedMentionDto>();

        foreach (var capture in linkedCaptures)
        {
            var personMention = capture.AiExtraction?.PeopleMentioned
                .FirstOrDefault(p => p.PersonId == personId);

            mentions.Add(new TranscriptMentionDto(
                capture.Id,
                capture.Title,
                capture.CapturedAt,
                capture.AiExtraction?.Summary,
                personMention?.Context));

            // Collect unresolved mentions from this capture
            if (capture.AiExtraction is not null)
            {
                foreach (var unresolved in capture.AiExtraction.PeopleMentioned
                    .Where(p => p.PersonId is null))
                {
                    unresolvedMentions.Add(new UnresolvedMentionDto(
                        capture.Id,
                        unresolved.RawName,
                        unresolved.Context));
                }
            }
        }

        // Build commitment DTOs
        var commitmentDtos = commitments.Select(c => new DossierCommitmentDto(
            c.Id,
            c.Description,
            c.Direction,
            c.DueDate,
            c.IsOverdue,
            c.Confidence)).ToList();

        // Build AI prompt context
        var mentionContexts = mentions.Select(m => new MentionContextForPrompt(
            m.CaptureTitle,
            m.CapturedAt,
            m.ExtractionSummary,
            m.MentionContext)).ToList();

        var commitmentContexts = commitments.Select(c => new CommitmentContextForPrompt(
            c.Description,
            c.Direction.ToString(),
            c.DueDate,
            c.IsOverdue)).ToList();

        // Call AI for synthesis
        var systemPrompt = DossierPromptBuilder.SystemPrompt(mode);
        var userPrompt = DossierPromptBuilder.BuildUserPrompt(
            person.Name,
            person.Role,
            person.Team,
            mentionContexts,
            commitmentContexts);

        var aiRequest = new AiCompletionRequest(
            systemPrompt,
            userPrompt,
            MaxTokens: 2048,
            Temperature: 0.3f);

        var aiResult = await aiCompletionService.CompleteAsync(aiRequest, cancellationToken);

        return new PersonDossierResponse(
            person.Id,
            person.Name,
            aiResult.Content,
            commitmentDtos,
            mentions,
            unresolvedMentions,
            DateTimeOffset.UtcNow);
    }
}
