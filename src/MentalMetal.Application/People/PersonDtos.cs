using MentalMetal.Domain.People;

namespace MentalMetal.Application.People;

public sealed record CreatePersonRequest(
    string Name,
    PersonType Type,
    string? Email = null,
    string? Role = null,
    string? Team = null,
    List<string>? Aliases = null);

public sealed record UpdatePersonRequest(
    string Name,
    string? Email = null,
    string? Role = null,
    string? Team = null,
    string? Notes = null,
    List<string>? Aliases = null);

public sealed record ChangeTypeRequest(PersonType NewType);

public sealed record SetAliasesRequest(List<string> Aliases);

public sealed record AddAliasRequest(string Alias);

public sealed record PersonResponse(
    Guid Id,
    Guid UserId,
    string Name,
    PersonType Type,
    string? Email,
    string? Role,
    string? Team,
    string? Notes,
    List<string> Aliases,
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
        person.Aliases.ToList(),
        person.IsArchived,
        person.ArchivedAt,
        person.CreatedAt,
        person.UpdatedAt);
}
