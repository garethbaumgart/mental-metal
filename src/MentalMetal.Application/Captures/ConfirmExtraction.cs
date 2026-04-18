using System.Globalization;
using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Users;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Initiatives;

namespace MentalMetal.Application.Captures;

public sealed class ConfirmExtractionHandler(
    ICaptureRepository captureRepository,
    ICommitmentRepository commitmentRepository,
    IPersonRepository personRepository,
    IInitiativeRepository initiativeRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<ConfirmExtractionResponse> HandleAsync(Guid captureId, CancellationToken cancellationToken)
    {
        var capture = (await captureRepository.GetByIdAsync(captureId, cancellationToken))
            .EnsureOwned(currentUserService.UserId, captureId);

        capture.ConfirmExtraction();

        var extraction = capture.AiExtraction!;
        var userId = currentUserService.UserId;
        var warnings = new List<string>();

        // Load all people and initiatives for matching
        var people = await personRepository.GetAllAsync(userId, null, false, cancellationToken);
        var initiatives = await initiativeRepository.GetAllAsync(userId, null, cancellationToken);

        // Spawn commitments
        foreach (var ec in extraction.Commitments)
        {
            var personId = MatchPerson(ec.PersonHint, people);
            if (personId == Guid.Empty)
            {
                var hint = string.IsNullOrWhiteSpace(ec.PersonHint) ? "(none)" : ec.PersonHint;
                warnings.Add($"Commitment skipped — no matching person for \"{hint}\": {ec.Description}");
                continue;
            }

            var direction = ec.Direction == ExtractionDirection.MineToThem
                ? CommitmentDirection.MineToThem
                : CommitmentDirection.TheirsToMe;

            DateOnly? dueDate = ParseIsoDate(ec.DueDate);

            var commitment = Commitment.Create(userId, ec.Description, direction, personId, dueDate,
                sourceCaptureId: capture.Id);
            await commitmentRepository.AddAsync(commitment, cancellationToken);
            capture.RecordSpawnedCommitment(commitment.Id);
        }

        // Auto-link matched people
        foreach (var personHint in extraction.SuggestedPersonLinks)
        {
            var personId = MatchPerson(personHint, people);
            if (personId != Guid.Empty)
                capture.LinkToPerson(personId);
        }

        // Auto-link matched initiatives
        foreach (var initiativeHint in extraction.SuggestedInitiativeLinks)
        {
            var initiativeId = MatchInitiative(initiativeHint, initiatives);
            if (initiativeId != Guid.Empty)
                capture.LinkToInitiative(initiativeId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ConfirmExtractionResponse(CaptureResponse.From(capture), warnings.AsReadOnly());
    }

    private static DateOnly? ParseIsoDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d : null;

    private static Guid MatchPerson(string? hint, IReadOnlyList<Person> people)
    {
        if (string.IsNullOrWhiteSpace(hint)) return Guid.Empty;

        var normalizedHint = hint.Trim().ToLower();

        // Exact match first
        var exact = people.FirstOrDefault(p => p.Name.ToLower() == normalizedHint);
        if (exact is not null) return exact.Id;

        // Contains match (e.g., "Sarah" matches "Sarah Chen")
        var partial = people.FirstOrDefault(p =>
            p.Name.ToLower().Contains(normalizedHint) ||
            normalizedHint.Contains(p.Name.ToLower()));
        if (partial is not null) return partial.Id;

        // First name match
        var firstName = people.FirstOrDefault(p =>
            p.Name.ToLower().Split(' ')[0] == normalizedHint);

        return firstName?.Id ?? Guid.Empty;
    }

    private static Guid MatchInitiative(string? hint, IReadOnlyList<Initiative> initiatives)
    {
        if (string.IsNullOrWhiteSpace(hint)) return Guid.Empty;

        var normalizedHint = hint.Trim().ToLower();

        var exact = initiatives.FirstOrDefault(i => i.Title.ToLower() == normalizedHint);
        if (exact is not null) return exact.Id;

        var partial = initiatives.FirstOrDefault(i =>
            i.Title.ToLower().Contains(normalizedHint) ||
            normalizedHint.Contains(i.Title.ToLower()));

        return partial?.Id ?? Guid.Empty;
    }
}
