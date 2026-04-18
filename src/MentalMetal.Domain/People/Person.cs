using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.People;

public sealed class Person : AggregateRoot, IUserScoped
{
    private readonly List<string> _aliases = [];

    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;
    public PersonType Type { get; private set; }
    public string? Email { get; private set; }
    public string? Role { get; private set; }
    public string? Team { get; private set; }
    public string? Notes { get; private set; }
    public IReadOnlyList<string> Aliases => _aliases;
    public bool IsArchived { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Person() { } // EF Core

    public static Person Create(
        Guid userId,
        string name,
        PersonType type,
        string? email = null,
        string? role = null,
        IEnumerable<string>? aliases = null)
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

        if (aliases is not null)
            person.SetAliasesInternal(aliases);

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
        Type = newType;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new PersonTypeChanged(Id, oldType, newType));
    }

    /// <summary>
    /// Replaces the full alias list. Enforces case-insensitive uniqueness within this person.
    /// Cross-person uniqueness per user is enforced at the application/repository level.
    /// </summary>
    public void SetAliases(IEnumerable<string> aliases)
    {
        SetAliasesInternal(aliases);
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new PersonAliasesUpdated(Id));
    }

    /// <summary>
    /// Adds a single alias. Case-insensitive duplicate within this person is rejected.
    /// </summary>
    public void AddAlias(string alias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias, nameof(alias));

        var trimmed = alias.Trim();

        if (_aliases.Any(a => string.Equals(a, trimmed, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"Alias '{trimmed}' already exists for this person.");

        _aliases.Add(trimmed);
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new PersonAliasesUpdated(Id));
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

    private void SetAliasesInternal(IEnumerable<string> aliases)
    {
        var filtered = aliases.ToList();
        if (filtered.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Aliases must not contain null or whitespace entries.");

        var trimmed = filtered.Select(a => a.Trim()).ToList();

        // Check for case-insensitive duplicates within the list
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var alias in trimmed)
        {
            if (!seen.Add(alias))
                throw new ArgumentException($"Duplicate alias '{alias}' in the provided list.");
        }

        _aliases.Clear();
        _aliases.AddRange(trimmed);
    }
}
