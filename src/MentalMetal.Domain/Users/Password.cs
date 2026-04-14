using MentalMetal.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace MentalMetal.Domain.Users;

public sealed class Password : ValueObject
{
    public const int MinimumLength = 8;

    public string HashValue { get; }

    private Password(string hashValue) => HashValue = hashValue;

    /// <summary>
    /// Creates a <see cref="Password"/> from a plaintext value by hashing it.
    /// </summary>
    public static Password Create(string plaintext, IPasswordHasher<User> hasher)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext, nameof(plaintext));
        ArgumentNullException.ThrowIfNull(hasher);

        if (plaintext.Length < MinimumLength)
            throw new ArgumentException(
                $"Password must be at least {MinimumLength} characters.",
                nameof(plaintext));

        // Identity's PasswordHasher<T> does not use the user instance for hashing.
        // The parameter exists for extensibility but is unused by the built-in PBKDF2 impl.
        var hash = hasher.HashPassword(null!, plaintext);
        return new Password(hash);
    }

    /// <summary>
    /// Rehydrates a <see cref="Password"/> from an already-computed hash string.
    /// Used by EF Core and when reading from persistence.
    /// </summary>
    public static Password CreateFromHash(string hashValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashValue, nameof(hashValue));
        return new Password(hashValue);
    }

    public bool Verify(string plaintext, IPasswordHasher<User> hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);

        if (string.IsNullOrEmpty(plaintext))
            return false;

        var result = hasher.VerifyHashedPassword(null!, HashValue, plaintext);
        return result == PasswordVerificationResult.Success
            || result == PasswordVerificationResult.SuccessRehashNeeded;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return HashValue;
    }
}
