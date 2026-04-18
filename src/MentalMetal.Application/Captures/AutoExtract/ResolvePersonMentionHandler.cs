using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Captures.AutoExtract;

public sealed record ResolvePersonMentionRequest(string RawName, Guid PersonId);

public sealed class ResolvePersonMentionHandler(
    ICaptureRepository captureRepository,
    IPersonRepository personRepository,
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

        var person = await personRepository.GetByIdAsync(request.PersonId, cancellationToken)
            ?? throw new InvalidOperationException($"Person not found: {request.PersonId}");

        if (person.UserId != userId)
            throw new InvalidOperationException($"Person not found: {request.PersonId}");

        // Add the raw name as an alias (idempotent — AddAlias throws on dup)
        var trimmedName = request.RawName.Trim();
        if (!string.Equals(person.Name, trimmedName, StringComparison.OrdinalIgnoreCase)
            && !person.Aliases.Any(a => string.Equals(a, trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            person.AddAlias(trimmedName);
        }

        // Update the extraction with resolved PersonId
        var updatedPeople = capture.AiExtraction.PeopleMentioned.Select(p =>
            string.Equals(p.RawName, request.RawName, StringComparison.OrdinalIgnoreCase)
                ? p with { PersonId = request.PersonId }
                : p).ToList();

        // Also update commitment PersonIds where the raw name matches
        var updatedCommitments = capture.AiExtraction.Commitments.ToList();

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
