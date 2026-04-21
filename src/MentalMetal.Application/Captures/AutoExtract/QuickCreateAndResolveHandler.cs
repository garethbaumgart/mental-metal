using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Captures.AutoExtract;

public sealed record QuickCreateAndResolveRequest(string RawName, string PersonName, PersonType PersonType);

public sealed class QuickCreateAndResolveHandler(
    ICaptureRepository captureRepository,
    IPersonRepository personRepository,
    ICommitmentRepository commitmentRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<CaptureResponse> HandleAsync(
        Guid captureId, QuickCreateAndResolveRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var capture = (await captureRepository.GetByIdAsync(captureId, cancellationToken))
            .EnsureOwned(userId, captureId);

        if (capture.AiExtraction is null)
            throw new InvalidOperationException("Capture has no AI extraction to resolve");

        var trimmedRawName = request.RawName.Trim();
        var trimmedPersonName = request.PersonName.Trim();

        // Validate that rawName matches an existing unresolved PeopleMentioned entry
        var mention = capture.AiExtraction.PeopleMentioned
            .FirstOrDefault(p => string.Equals(p.RawName, trimmedRawName, StringComparison.OrdinalIgnoreCase));
        if (mention is null)
            throw new InvalidOperationException(
                $"No person mention with raw name '{trimmedRawName}' found in extraction.");

        if (mention.PersonId.HasValue)
            throw new InvalidOperationException(
                $"Person mention '{trimmedRawName}' is already resolved.");

        // Check for duplicate person name
        if (await personRepository.ExistsByNameAsync(userId, trimmedPersonName, excludeId: null, cancellationToken))
            throw new DuplicatePersonNameException(trimmedPersonName);

        // Create the person
        var person = Person.Create(userId, trimmedPersonName, request.PersonType);

        // Add raw name as alias if different from person name
        if (!string.Equals(trimmedRawName, trimmedPersonName, StringComparison.OrdinalIgnoreCase))
        {
            // Check alias uniqueness across user's people
            if (await personRepository.AliasExistsForOtherPersonAsync(userId, trimmedRawName, person.Id, cancellationToken))
                throw new AliasConflictException(trimmedRawName);

            person.AddAlias(trimmedRawName);
        }

        await personRepository.AddAsync(person, cancellationToken);

        // Update the extraction with resolved PersonId on people mentions
        var updatedPeople = capture.AiExtraction.PeopleMentioned.Select(p =>
            string.Equals(p.RawName, trimmedRawName, StringComparison.OrdinalIgnoreCase)
                ? p with { PersonId = person.Id }
                : p).ToList();

        // Spawn skipped commitments for this person and update extraction commitments
        var updatedCommitments = new List<ExtractedCommitment>();
        foreach (var c in capture.AiExtraction.Commitments)
        {
            if (string.Equals(c.PersonRawName, trimmedRawName, StringComparison.OrdinalIgnoreCase)
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
                    person.Id,
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
                    PersonId = person.Id,
                    SpawnedCommitmentId = commitment.Id
                });
            }
            else if (string.Equals(c.PersonRawName, trimmedRawName, StringComparison.OrdinalIgnoreCase))
            {
                updatedCommitments.Add(c with { PersonId = person.Id });
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

        capture.UpdateExtraction(updatedExtraction);
        capture.LinkToPerson(person.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CaptureResponse.From(capture);
    }
}

/// <summary>
/// Thrown when a person with the same name already exists for the user.
/// </summary>
public sealed class DuplicatePersonNameException(string name)
    : Exception($"A person named '{name}' already exists. Consider linking to the existing person instead.")
{
    public string PersonName { get; } = name;
}

/// <summary>
/// Thrown when the raw name is already used as an alias by another person.
/// </summary>
public sealed class AliasConflictException(string alias)
    : Exception($"Alias '{alias}' is already used by another person.")
{
    public string Alias { get; } = alias;
}
