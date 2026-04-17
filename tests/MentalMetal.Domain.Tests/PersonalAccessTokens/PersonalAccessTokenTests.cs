using MentalMetal.Domain.PersonalAccessTokens;

namespace MentalMetal.Domain.Tests.PersonalAccessTokens;

public class PersonalAccessTokenTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly byte[] TestHash = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32];
    private static readonly byte[] TestPrefix = [1, 2, 3, 4, 5, 6, 7, 8];

    private static PersonalAccessToken CreateToken(string name = "Test token") =>
        PersonalAccessToken.Create(UserId, name, ["captures:write"], TestHash, TestPrefix);

    [Fact]
    public void Create_ValidInputs_CreatesTokenWithCorrectState()
    {
        var token = CreateToken();

        Assert.NotEqual(Guid.Empty, token.Id);
        Assert.Equal(UserId, token.UserId);
        Assert.Equal("Test token", token.Name);
        Assert.Contains("captures:write", token.Scopes);
        Assert.Equal(TestHash, token.TokenHash);
        Assert.Equal(TestPrefix, token.TokenLookupPrefix);
        Assert.True(token.IsActive);
        Assert.Null(token.LastUsedAt);
        Assert.Null(token.RevokedAt);

        var domainEvent = Assert.Single(token.DomainEvents);
        var created = Assert.IsType<PersonalAccessTokenCreated>(domainEvent);
        Assert.Equal(token.Id, created.TokenId);
        Assert.Equal(UserId, created.UserId);
        Assert.Equal("Test token", created.Name);
    }

    [Fact]
    public void Create_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            PersonalAccessToken.Create(Guid.Empty, "name", ["captures:write"], TestHash, TestPrefix));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyName_Throws(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            PersonalAccessToken.Create(UserId, name!, ["captures:write"], TestHash, TestPrefix));
    }

    [Fact]
    public void Create_EmptyScopes_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            PersonalAccessToken.Create(UserId, "name", [], TestHash, TestPrefix));
    }

    [Fact]
    public void Create_EmptyTokenHash_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            PersonalAccessToken.Create(UserId, "name", ["captures:write"], [], TestPrefix));
    }

    [Fact]
    public void Create_EmptyTokenLookupPrefix_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            PersonalAccessToken.Create(UserId, "name", ["captures:write"], TestHash, []));
    }

    [Fact]
    public void Create_TrimsName()
    {
        var token = PersonalAccessToken.Create(UserId, "  spaced  ", ["captures:write"], TestHash, TestPrefix);
        Assert.Equal("spaced", token.Name);
    }

    [Fact]
    public void Revoke_ActiveToken_SetsRevokedAtAndRaisesEvent()
    {
        var token = CreateToken();
        token.ClearDomainEvents();

        token.Revoke();

        Assert.NotNull(token.RevokedAt);
        Assert.False(token.IsActive);
        var domainEvent = Assert.Single(token.DomainEvents);
        var revoked = Assert.IsType<PersonalAccessTokenRevoked>(domainEvent);
        Assert.Equal(token.Id, revoked.TokenId);
        Assert.Equal(UserId, revoked.UserId);
    }

    [Fact]
    public void Revoke_AlreadyRevoked_IsIdempotent()
    {
        var token = CreateToken();
        token.Revoke();
        var firstRevokedAt = token.RevokedAt;
        token.ClearDomainEvents();

        token.Revoke();

        Assert.Equal(firstRevokedAt, token.RevokedAt);
        Assert.Empty(token.DomainEvents);
    }

    [Fact]
    public void TouchLastUsed_UpdatesTimestamp()
    {
        var token = CreateToken();
        Assert.Null(token.LastUsedAt);

        token.TouchLastUsed();

        Assert.NotNull(token.LastUsedAt);
    }
}
