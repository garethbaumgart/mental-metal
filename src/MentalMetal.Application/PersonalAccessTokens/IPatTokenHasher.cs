namespace MentalMetal.Application.PersonalAccessTokens;

public interface IPatTokenHasher
{
    /// <summary>
    /// Hashes a plaintext token and returns the full hash plus a short lookup prefix.
    /// </summary>
    (byte[] Hash, byte[] LookupPrefix) HashToken(string plaintext);

    /// <summary>
    /// Constant-time comparison of a plaintext token against a stored hash.
    /// </summary>
    bool Verify(string plaintext, byte[] storedHash);
}
