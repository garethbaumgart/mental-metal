using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.PersonalAccessTokens;

public sealed class PersonalAccessToken : AggregateRoot, IUserScoped
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;
    public HashSet<string> Scopes { get; private set; } = [];
    public byte[] TokenHash { get; private set; } = [];
    public byte[] TokenLookupPrefix { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    private PersonalAccessToken() { } // EF Core

    public static PersonalAccessToken Create(
        Guid userId,
        string name,
        HashSet<string> scopes,
        byte[] tokenHash,
        byte[] tokenLookupPrefix)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(scopes, nameof(scopes));
        if (scopes.Count == 0)
            throw new ArgumentException("At least one scope is required.", nameof(scopes));
        ArgumentNullException.ThrowIfNull(tokenHash, nameof(tokenHash));
        if (tokenHash.Length == 0)
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        ArgumentNullException.ThrowIfNull(tokenLookupPrefix, nameof(tokenLookupPrefix));
        if (tokenLookupPrefix.Length == 0)
            throw new ArgumentException("Token lookup prefix is required.", nameof(tokenLookupPrefix));

        var token = new PersonalAccessToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            Scopes = scopes,
            TokenHash = tokenHash,
            TokenLookupPrefix = tokenLookupPrefix,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        token.RaiseDomainEvent(new PersonalAccessTokenCreated(token.Id, userId, token.Name));
        return token;
    }

    public bool IsActive => RevokedAt is null;

    public void Revoke()
    {
        if (RevokedAt is not null)
            return; // Idempotent

        RevokedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new PersonalAccessTokenRevoked(Id, UserId));
    }

    public void TouchLastUsed()
    {
        LastUsedAt = DateTimeOffset.UtcNow;
    }
}
