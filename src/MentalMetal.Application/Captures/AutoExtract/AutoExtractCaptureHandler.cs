using System.Globalization;
using System.Text.Json;
using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.Logging;

namespace MentalMetal.Application.Captures.AutoExtract;

/// <summary>
/// Orchestrates the full auto-extraction pipeline:
///   1. Call AI to extract structured data from capture content
///   2. Resolve person mentions against the user's People
///   3. Tag initiatives
///   4. Spawn commitments for High/Medium confidence items
///   5. Persist everything
/// </summary>
public sealed class AutoExtractCaptureHandler(
    ICaptureRepository captureRepository,
    IPersonRepository personRepository,
    IInitiativeRepository initiativeRepository,
    ICommitmentRepository commitmentRepository,
    IAiCompletionService aiCompletionService,
    ITasteBudgetService tasteBudgetService,
    ICurrentUserService currentUserService,
    NameResolutionService nameResolution,
    InitiativeTaggingService initiativeTagging,
    IUnitOfWork unitOfWork,
    ILogger<AutoExtractCaptureHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Runs the full extraction pipeline for the given capture.
    /// The capture must be in Raw status and belong to the current user.
    /// </summary>
    public async Task<CaptureResponse> HandleAsync(Guid captureId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var capture = (await captureRepository.GetByIdAsync(captureId, cancellationToken))
            .EnsureOwned(userId, captureId);

        if (capture.ProcessingStatus != ProcessingStatus.Raw)
            throw new InvalidOperationException(
                $"Capture {captureId} is not in Raw status — currently '{capture.ProcessingStatus}'.");

        capture.BeginProcessing();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            // Check taste budget
            if (tasteBudgetService.IsEnabled)
                await tasteBudgetService.DecrementAsync(userId, cancellationToken);

            // 1. Call AI provider
            var aiRequest = new AiCompletionRequest(
                ExtractionPromptBuilder.SystemPrompt,
                ExtractionPromptBuilder.BuildUserPrompt(capture.RawContent),
                MaxTokens: 4096,
                Temperature: 0.1f);

            var aiResult = await aiCompletionService.CompleteAsync(aiRequest, cancellationToken);

            var cleaned = JsonResponseParser.StripCodeFences(aiResult.Content);
            var dto = JsonSerializer.Deserialize<ExtractionResponseDto>(cleaned, JsonOptions)
                ?? throw new InvalidOperationException("AI returned null extraction result.");

            // 2. Load user's people and initiatives for resolution
            var people = await personRepository.GetAllAsync(userId, null, false, cancellationToken);
            var initiatives = await initiativeRepository.GetAllAsync(userId, InitiativeStatus.Active, cancellationToken);

            // 3. Resolve person names
            var rawNames = dto.PeopleMentioned.Select(p => p.RawName).ToList();
            // Also include person names from commitments
            var commitmentPersonNames = dto.Commitments
                .Where(c => !string.IsNullOrWhiteSpace(c.PersonRawName))
                .Select(c => c.PersonRawName!)
                .ToList();
            var allNames = rawNames.Union(commitmentPersonNames, StringComparer.OrdinalIgnoreCase).ToList();
            var nameResolutions = nameResolution.Resolve(allNames, people);

            // 4. Resolve initiative tags
            var rawInitiativeNames = dto.InitiativeTags.Select(t => t.RawName).ToList();
            var initiativeResolutions = initiativeTagging.Resolve(rawInitiativeNames, initiatives);

            // 5. Build PersonMention list with resolved IDs
            var peopleMentioned = dto.PeopleMentioned.Select(p => new PersonMention
            {
                RawName = p.RawName,
                PersonId = nameResolutions.GetValueOrDefault(p.RawName.Trim()),
                Context = p.Context
            }).ToList();

            // 6. Build InitiativeTag list with resolved IDs
            var initiativeTags = dto.InitiativeTags.Select(t => new InitiativeTag
            {
                RawName = t.RawName,
                InitiativeId = initiativeResolutions.GetValueOrDefault(t.RawName.Trim()),
                Context = t.Context
            }).ToList();

            // 7. Build ExtractedCommitment list and spawn commitments for High/Medium
            var extractedCommitments = new List<ExtractedCommitment>();
            foreach (var c in dto.Commitments)
            {
                if (!TryParseConfidence(c.Confidence, out var confidence)
                    || !TryParseDirection(c.Direction, out var direction))
                {
                    logger.LogWarning(
                        "Skipping commitment with invalid Direction={Direction} or Confidence={Confidence}",
                        c.Direction, c.Confidence);
                    continue;
                }
                var personId = !string.IsNullOrWhiteSpace(c.PersonRawName)
                    ? nameResolutions.GetValueOrDefault(c.PersonRawName.Trim())
                    : null;
                var dueDate = ParseDueDate(c.DueDate);

                Guid? spawnedCommitmentId = null;

                // Spawn commitment entity for High/Medium confidence with a resolved person
                if (confidence is CommitmentConfidence.High or CommitmentConfidence.Medium
                    && personId.HasValue)
                {
                    var dueDateOnly = dueDate.HasValue
                        ? DateOnly.FromDateTime(dueDate.Value.UtcDateTime)
                        : (DateOnly?)null;

                    // Initiative linking is handled via the capture's linked initiatives;
                    // don't guess which initiative a commitment belongs to.
                    var commitment = Commitment.Create(
                        userId,
                        c.Description,
                        direction,
                        personId.Value,
                        dueDateOnly,
                        initiativeId: null,
                        capture.Id,
                        confidence,
                        c.SourceStartOffset,
                        c.SourceEndOffset);

                    await commitmentRepository.AddAsync(commitment, cancellationToken);
                    spawnedCommitmentId = commitment.Id;
                    capture.RecordSpawnedCommitment(commitment.Id);
                }

                extractedCommitments.Add(new ExtractedCommitment
                {
                    Description = c.Description,
                    Direction = direction,
                    PersonId = personId,
                    DueDate = dueDate,
                    Confidence = confidence,
                    SourceStartOffset = c.SourceStartOffset,
                    SourceEndOffset = c.SourceEndOffset,
                    SpawnedCommitmentId = spawnedCommitmentId
                });
            }

            // 8. Auto-link capture to resolved people
            foreach (var (_, personId) in nameResolutions)
            {
                if (personId.HasValue)
                    capture.LinkToPerson(personId.Value);
            }

            // 9. Auto-link capture to resolved initiatives
            foreach (var (_, initiativeId) in initiativeResolutions)
            {
                if (initiativeId.HasValue)
                    capture.LinkToInitiative(initiativeId.Value);
            }

            // 10. Complete processing
            var extraction = new AiExtraction
            {
                Summary = dto.Summary,
                PeopleMentioned = peopleMentioned,
                Commitments = extractedCommitments,
                Decisions = dto.Decisions,
                Risks = dto.Risks,
                InitiativeTags = initiativeTags,
                ExtractedAt = DateTimeOffset.UtcNow
            };

            capture.CompleteProcessing(extraction);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return CaptureResponse.From(capture);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Auto-extraction failed for capture {CaptureId}", captureId);

            // Discard partial changes from the failed extraction attempt
            unitOfWork.DiscardPendingChanges();

            // Re-load the capture fresh (it was saved as Processing earlier)
            var freshCapture = (await captureRepository.GetByIdAsync(captureId, CancellationToken.None))!;
            freshCapture.FailProcessing(ex.Message);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);

            return CaptureResponse.From(freshCapture);
        }
    }

    private static bool TryParseConfidence(string? value, out CommitmentConfidence result)
    {
        switch (value)
        {
            case "High":
                result = CommitmentConfidence.High;
                return true;
            case "Medium":
                result = CommitmentConfidence.Medium;
                return true;
            case "Low":
                result = CommitmentConfidence.Low;
                return true;
            default:
                result = default;
                return false;
        }
    }

    private static bool TryParseDirection(string? value, out CommitmentDirection result)
    {
        switch (value)
        {
            case "MineToThem":
                result = CommitmentDirection.MineToThem;
                return true;
            case "TheirsToMe":
                result = CommitmentDirection.TheirsToMe;
                return true;
            default:
                result = default;
                return false;
        }
    }

    private static DateTimeOffset? ParseDueDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result)
            ? result
            : null;
    }
}
