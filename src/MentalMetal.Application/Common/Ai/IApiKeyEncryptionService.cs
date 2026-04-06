namespace MentalMetal.Application.Common.Ai;

public interface IApiKeyEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
