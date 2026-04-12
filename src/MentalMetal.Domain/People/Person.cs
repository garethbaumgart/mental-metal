using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.People;

public sealed class Person : AggregateRoot, IUserScoped
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;
    public PersonType Type { get; private set; }
    public string? Email { get; private set; }
    public string? Role { get; private set; }
    public string? Team { get; private set; }
    public string? Notes { get; private set; }
    public CareerDetails? CareerDetails { get; private set; }
    public CandidateDetails? CandidateDetails { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Person() { } // EF Core

    public static Person Create(Guid userId, string name, PersonType type, string? email = null, string? role = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        var now = DateTimeOffset.UtcNow;

        var person = new Person
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            Type = type,
            Email = email?.Trim(),
            Role = role?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        if (type == PersonType.Candidate)
            person.CandidateDetails = CandidateDetails.Create();

        person.RaiseDomainEvent(new PersonCreated(person.Id, userId, person.Name, type));

        return person;
    }

    public void UpdateProfile(string name, string? email, string? role, string? team, string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        Name = name.Trim();
        Email = email?.Trim();
        Role = role?.Trim();
        Team = team?.Trim();
        Notes = notes?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new PersonProfileUpdated(Id));
    }

    public void ChangeType(PersonType newType)
    {
        if (Type == newType)
            return;

        var oldType = Type;

        // Clear type-specific details from old type
        if (oldType == PersonType.DirectReport)
            CareerDetails = null;

        if (oldType == PersonType.Candidate)
            CandidateDetails = null;

        Type = newType;

        // Initialise type-specific details for new type
        if (newType == PersonType.Candidate)
            CandidateDetails = CandidateDetails.Create();

        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new PersonTypeChanged(Id, oldType, newType));
    }

    public void UpdateCareerDetails(string? level, string? aspirations, string? growthAreas)
    {
        if (Type != PersonType.DirectReport)
            throw new ArgumentException("Career details are only valid for direct reports.");

        CareerDetails = CareerDetails.Create(level, aspirations, growthAreas);
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CareerDetailsUpdated(Id));
    }

    public void UpdateCandidateDetails(string? cvNotes, string? sourceChannel)
    {
        if (Type != PersonType.Candidate)
            throw new ArgumentException("Candidate details are only valid for candidates.");

        CandidateDetails = CandidateDetails.Create(
            CandidateDetails!.PipelineStatus,
            cvNotes,
            sourceChannel);
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CandidateDetailsUpdated(Id));
    }

    public void AdvanceCandidatePipeline(PipelineStatus newStatus)
    {
        if (Type != PersonType.Candidate)
            throw new ArgumentException("Pipeline advancement is only valid for candidates.");

        var oldStatus = CandidateDetails!.PipelineStatus;
        CandidateDetails.ValidateTransition(oldStatus, newStatus);

        CandidateDetails = CandidateDetails.Create(
            newStatus,
            CandidateDetails.CvNotes,
            CandidateDetails.SourceChannel);
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CandidatePipelineAdvanced(Id, oldStatus, newStatus));
    }

    public void Archive()
    {
        if (IsArchived)
            return;

        IsArchived = true;
        ArchivedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new PersonArchived(Id));
    }
}
