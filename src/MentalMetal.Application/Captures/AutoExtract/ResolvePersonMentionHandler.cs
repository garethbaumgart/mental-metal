using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Captures.AutoExtract;

public sealed record ResolvePersonMentionRequest(string RawName, Guid PersonId);

public sealed class ResolvePersonMentionHandler(
    ICaptureRepository captureRepository,
    IPersonRepository personRepository,
    ICommitmentRepository commitmentRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<CaptureResponse> HandleAsync(
        Guid captureId, ResolvePersonMentionRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var capture = (await captureRepository.GetByIdAsync(captureId, cancellationToken))
            .EnsureOwned(userId, captureId);

        if (capture.AiExtraction is null)
            throw new InvalidOperationException("Capture has no AI extraction to resolve.");

        // Validate that rawName matches an existing unresolved PeopleMentioned entry
        var trimmedName = request.RawName.Trim();
        var mention = capture.AiExtraction.PeopleMentioned
            .FirstOrDefault(p => string.Equals(p.RawName, trimmedName, StringComparison.OrdinalIgnoreCase));
        if (mention is null)
            throw new InvalidOperationException(
                $"No person mention with raw name '{trimmedName}' found in extraction.");

        if (mention.PersonId.HasValue)
            throw new InvalidOperationException(
                $"Person mention '{trimmedName}' is already resolved.");

        var person = await personRepository.GetByIdAsync(request.PersonId, cancellationToken)
            ?? throw new InvalidOperationException($"Person not found: {request.PersonId}");

        if (person.UserId != userId)
            throw new InvalidOperationException($"Person not found: {request.PersonId}");

        // Add the raw name as an alias (idempotent — skip if already this person's name/alias)
        if (!string.Equals(person.Name, trimmedName, StringComparison.OrdinalIgnoreCase)
            && !person.Aliases.Any(a => string.Equals(a, trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            // Ensure alias isn't already used by another person
            if (await personRepository.AliasExistsForOtherPersonAsync(userId, trimmedName, person.Id, cancellationToken))
                throw new AliasConflictException(trimmedName);

            person.AddAlias(trimmedName);
        }

        // Update the extraction with resolved PersonId on people mentions
        var updatedPeople = capture.AiExtraction.PeopleMentioned.Select(p =>
            string.Equals(p.RawName, trimmedName, StringComparison.OrdinalIgnoreCase)
                ? p with { PersonId = request.PersonId }
                : p).ToList();

        // Spawn skipped commitments for this person and update extraction commitments
        var updatedCommitments = new List<ExtractedCommitment>();
        foreach (var c in capture.AiExtraction.Commitments)
        {
            if (string.Equals(c.PersonRawName, trimmedName, StringComparison.OrdinalIgnoreCase)
                && c.SpawnedCommitmentId is null
                && c.Confidence is CommitmentConfidence.High or CommitmentConfidence.Medium)
            {
                var dueDateOnly = c.DueDate.HasValue
                    ? DateOnly.FromDateTime(c.DueDate.Value.UtcDateTime)
                    : (DateOnly?)null;

                var commitment = Commitment.Create(
                    userId,
                    c.Description,
                    c.Direction,
                    request.PersonId,
                    dueDateOnly,
                    initiativeId: null,
                    capture.Id,
                    c.Confidence,
                    c.SourceStartOffset,
                    c.SourceEndOffset);

                await commitmentRepository.AddAsync(commitment, cancellationToken);
                capture.RecordSpawnedCommitment(commitment.Id);

                updatedCommitments.Add(c with
                {
                    PersonId = request.PersonId,
                    SpawnedCommitmentId = commitment.Id
                });
            }
            else if (string.Equals(c.PersonRawName, trimmedName, StringComparison.OrdinalIgnoreCase))
            {
                // Update PersonId on already-spawned or low-confidence commitments
                updatedCommitments.Add(c with { PersonId = request.PersonId });
            }
            else
            {
                updatedCommitments.Add(c);
            }
        }

        var updatedExtraction = capture.AiExtraction with
        {
            PeopleMentioned = updatedPeople,
            Commitments = updatedCommitments
        };

        // Replace extraction via domain method
        capture.UpdateExtraction(updatedExtraction);

        // Link capture to the person
        capture.LinkToPerson(request.PersonId);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CaptureResponse.From(capture);
    }
}
