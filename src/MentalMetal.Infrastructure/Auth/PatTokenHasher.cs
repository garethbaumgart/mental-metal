using System.Security.Cryptography;
using System.Text;
using MentalMetal.Application.PersonalAccessTokens;
using Microsoft.Extensions.Configuration;

namespace MentalMetal.Infrastructure.Auth;

public sealed class PatTokenHasher : IPatTokenHasher
{
    private const int LookupPrefixLength = 8;
    private readonly Lazy<byte[]> _pepper;

    public PatTokenHasher(IConfiguration configuration)
    {
        _pepper = new Lazy<byte[]>(() =>
        {
            var pepper = configuration["PersonalAccessTokens:Pepper"]
                ?? configuration["AiProvider:EncryptionKey"]
                ?? throw new InvalidOperationException(
                    "PersonalAccessTokens:Pepper or AiProvider:EncryptionKey must be configured.");
            return Encoding.UTF8.GetBytes(pepper);
        });
    }

    public (byte[] Hash, byte[] LookupPrefix) HashToken(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext, nameof(plaintext));

        var hash = ComputeHash(plaintext);
        var prefix = hash[..LookupPrefixLength];
        return (hash, prefix);
    }

    public bool Verify(string plaintext, byte[] storedHash)
    {
        var computed = ComputeHash(plaintext);
        return CryptographicOperations.FixedTimeEquals(computed, storedHash);
    }

    private byte[] ComputeHash(string plaintext)
    {
        var pepper = _pepper.Value;
        var input = new byte[pepper.Length + Encoding.UTF8.GetByteCount(plaintext)];
        pepper.CopyTo(input, 0);
        Encoding.UTF8.GetBytes(plaintext, input.AsSpan(pepper.Length));
        return SHA256.HashData(input);
    }
}
