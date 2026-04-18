using MentalMetal.Domain.People;

namespace MentalMetal.Domain.Tests.People;

public class PersonTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Theory]
    [InlineData(PersonType.DirectReport)]
    [InlineData(PersonType.Stakeholder)]
    [InlineData(PersonType.Peer)]
    [InlineData(PersonType.External)]
    public void Create_ValidInputs_CreatesPersonWithCorrectState(PersonType type)
    {
        var person = Person.Create(UserId, "Alice Smith", type, "alice@example.com", "Engineer");

        Assert.NotEqual(Guid.Empty, person.Id);
        Assert.Equal(UserId, person.UserId);
        Assert.Equal("Alice Smith", person.Name);
        Assert.Equal(type, person.Type);
        Assert.Equal("alice@example.com", person.Email);
        Assert.Equal("Engineer", person.Role);
        Assert.Null(person.Team);
        Assert.Null(person.Notes);
        Assert.Empty(person.Aliases);
        Assert.False(person.IsArchived);
        Assert.Null(person.ArchivedAt);
    }

    [Fact]
    public void Create_WithAliases_SetsAliases()
    {
        var person = Person.Create(UserId, "Alice Smith", PersonType.DirectReport,
            aliases: ["Ali", "AJ"]);

        Assert.Equal(2, person.Aliases.Count);
        Assert.Contains("Ali", person.Aliases);
        Assert.Contains("AJ", person.Aliases);
    }

    [Fact]
    public void Create_RaisesPersonCreatedEvent()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport);

        var domainEvent = Assert.Single(person.DomainEvents);
        var created = Assert.IsType<PersonCreated>(domainEvent);
        Assert.Equal(person.Id, created.PersonId);
        Assert.Equal(UserId, created.UserId);
        Assert.Equal("Alice", created.Name);
        Assert.Equal(PersonType.DirectReport, created.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyName_Throws(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Person.Create(UserId, name!, PersonType.DirectReport));
    }

    [Fact]
    public void UpdateProfile_SetsFieldsAndRaisesEvent()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport);
        person.ClearDomainEvents();

        person.UpdateProfile("Alice Updated", "alice@new.com", "Senior Engineer", "Platform", "Great performer");

        Assert.Equal("Alice Updated", person.Name);
        Assert.Equal("alice@new.com", person.Email);
        Assert.Equal("Senior Engineer", person.Role);
        Assert.Equal("Platform", person.Team);
        Assert.Equal("Great performer", person.Notes);

        var domainEvent = Assert.Single(person.DomainEvents);
        Assert.IsType<PersonProfileUpdated>(domainEvent);
    }

    [Fact]
    public void ChangeType_SameType_IsNoOp()
    {
        var person = Person.Create(UserId, "Alice", PersonType.Stakeholder);
        person.ClearDomainEvents();

        person.ChangeType(PersonType.Stakeholder);

        Assert.Empty(person.DomainEvents);
    }

    [Fact]
    public void ChangeType_ChangesTypeAndRaisesEvent()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport);
        person.ClearDomainEvents();

        person.ChangeType(PersonType.Peer);

        Assert.Equal(PersonType.Peer, person.Type);
        var domainEvent = Assert.Single(person.DomainEvents);
        var changed = Assert.IsType<PersonTypeChanged>(domainEvent);
        Assert.Equal(PersonType.DirectReport, changed.OldType);
        Assert.Equal(PersonType.Peer, changed.NewType);
    }

    // --- Alias invariant tests ---

    [Fact]
    public void SetAliases_ReplacesFullList()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport, aliases: ["Old"]);
        person.ClearDomainEvents();

        person.SetAliases(["New1", "New2"]);

        Assert.Equal(2, person.Aliases.Count);
        Assert.Contains("New1", person.Aliases);
        Assert.Contains("New2", person.Aliases);
        Assert.DoesNotContain("Old", person.Aliases);

        var domainEvent = Assert.Single(person.DomainEvents);
        Assert.IsType<PersonAliasesUpdated>(domainEvent);
    }

    [Fact]
    public void SetAliases_RejectsCaseInsensitiveDuplicates()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport);

        Assert.ThrowsAny<ArgumentException>(() =>
            person.SetAliases(["Ali", "ali"]));
    }

    [Fact]
    public void AddAlias_AddsToExistingList()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport, aliases: ["Ali"]);
        person.ClearDomainEvents();

        person.AddAlias("AJ");

        Assert.Equal(2, person.Aliases.Count);
        Assert.Contains("Ali", person.Aliases);
        Assert.Contains("AJ", person.Aliases);

        var domainEvent = Assert.Single(person.DomainEvents);
        Assert.IsType<PersonAliasesUpdated>(domainEvent);
    }

    [Fact]
    public void AddAlias_RejectsCaseInsensitiveDuplicate()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport, aliases: ["Ali"]);

        Assert.ThrowsAny<ArgumentException>(() =>
            person.AddAlias("ali"));
    }

    [Fact]
    public void AddAlias_RejectsEmptyAlias()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport);

        Assert.ThrowsAny<ArgumentException>(() =>
            person.AddAlias(""));
    }

    [Fact]
    public void SetAliases_TrimsWhitespace()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport);
        person.SetAliases(["  Ali  ", "AJ "]);

        Assert.Equal("Ali", person.Aliases[0]);
        Assert.Equal("AJ", person.Aliases[1]);
    }

    [Fact]
    public void SetAliases_ThrowsOnEmptyStrings()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport);

        Assert.Throws<ArgumentException>(() => person.SetAliases(["Ali", "", "AJ"]));
    }

    [Fact]
    public void SetAliases_ThrowsOnWhitespaceStrings()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport);

        Assert.Throws<ArgumentException>(() => person.SetAliases(["Ali", "  ", "AJ"]));
    }

    [Fact]
    public void Archive_SetsIsArchivedAndRaisesEvent()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport);
        person.ClearDomainEvents();

        person.Archive();

        Assert.True(person.IsArchived);
        Assert.NotNull(person.ArchivedAt);

        var domainEvent = Assert.Single(person.DomainEvents);
        Assert.IsType<PersonArchived>(domainEvent);
    }

    [Fact]
    public void Archive_AlreadyArchived_IsIdempotent()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport);
        person.Archive();
        person.ClearDomainEvents();

        person.Archive();

        Assert.True(person.IsArchived);
        Assert.Empty(person.DomainEvents);
    }
}
