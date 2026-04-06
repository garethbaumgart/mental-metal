using System.Security.Cryptography;
using MentalMetal.Application.Common.Ai;
using Microsoft.Extensions.Options;

namespace MentalMetal.Infrastructure.Ai;

public sealed class AesApiKeyEncryptionService(IOptions<AiProviderSettings> settings) : IApiKeyEncryptionService
{
    private readonly byte[] _key = ValidateKey(settings.Value.EncryptionKey);

    private static byte[] ValidateKey(string base64Key)
    {
        byte[] key;
        try
        {
            key = Convert.FromBase64String(base64Key);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "AiProvider:EncryptionKey is not valid Base64. Generate a 32-byte key: openssl rand -base64 32");
        }

        if (key.Length != 32)
            throw new InvalidOperationException(
                $"AiProvider:EncryptionKey must be exactly 32 bytes (AES-256). Got {key.Length} bytes. Generate with: openssl rand -base64 32");

        return key;
    }

    public string Encrypt(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext, nameof(plaintext));

        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        RandomNumberGenerator.Fill(nonce);

        var plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes

        using var aes = new AesGcm(_key, tag.Length);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Format: base64(nonce):base64(ciphertext):base64(tag)
        return $"{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(ciphertext)}:{Convert.ToBase64String(tag)}";
    }

    public string Decrypt(string encrypted)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encrypted, nameof(encrypted));

        var parts = encrypted.Split(':');
        if (parts.Length != 3)
            throw new FormatException("Invalid encrypted key format. Expected nonce:ciphertext:tag.");

        var nonce = Convert.FromBase64String(parts[0]);
        var ciphertext = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);

        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, tag.Length);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return System.Text.Encoding.UTF8.GetString(plaintext);
    }
}
