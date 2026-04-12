using MentalMetal.Domain.People;

namespace MentalMetal.Application.People;

public sealed record CreatePersonRequest(
    string Name,
    PersonType Type,
    string? Email = null,
    string? Role = null,
    string? Team = null);

public sealed record UpdatePersonRequest(
    string Name,
    string? Email = null,
    string? Role = null,
    string? Team = null,
    string? Notes = null);

public sealed record ChangeTypeRequest(PersonType NewType);

public sealed record CareerDetailsRequest(
    string? Level = null,
    string? Aspirations = null,
    string? GrowthAreas = null);

public sealed record CandidateDetailsRequest(
    string? CvNotes = null,
    string? SourceChannel = null);

public sealed record AdvancePipelineRequest(PipelineStatus NewStatus);

public sealed record PersonResponse(
    Guid Id,
    Guid UserId,
    string Name,
    PersonType Type,
    string? Email,
    string? Role,
    string? Team,
    string? Notes,
    CareerDetailsResponse? CareerDetails,
    CandidateDetailsResponse? CandidateDetails,
    bool IsArchived,
    DateTimeOffset? ArchivedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static PersonResponse From(Person person) => new(
        person.Id,
        person.UserId,
        person.Name,
        person.Type,
        person.Email,
        person.Role,
        person.Team,
        person.Notes,
        person.CareerDetails is not null
            ? new CareerDetailsResponse(
                person.CareerDetails.Level,
                person.CareerDetails.Aspirations,
                person.CareerDetails.GrowthAreas)
            : null,
        person.CandidateDetails is not null
            ? new CandidateDetailsResponse(
                person.CandidateDetails.PipelineStatus,
                person.CandidateDetails.CvNotes,
                person.CandidateDetails.SourceChannel)
            : null,
        person.IsArchived,
        person.ArchivedAt,
        person.CreatedAt,
        person.UpdatedAt);
}

public sealed record CareerDetailsResponse(
    string? Level,
    string? Aspirations,
    string? GrowthAreas);

public sealed record CandidateDetailsResponse(
    PipelineStatus PipelineStatus,
    string? CvNotes,
    string? SourceChannel);
