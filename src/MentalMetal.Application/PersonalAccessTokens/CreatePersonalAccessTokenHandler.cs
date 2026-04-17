using System.Security.Cryptography;
using MentalMetal.Application.Common;
using MentalMetal.Domain.PersonalAccessTokens;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.PersonalAccessTokens;

public sealed class CreatePersonalAccessTokenHandler(
    IPersonalAccessTokenRepository repository,
    ICurrentUserService currentUserService,
    IPatTokenHasher hasher,
    IUnitOfWork unitOfWork)
{
    public async Task<PatCreatedResponse> HandleAsync(CreatePatRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name, nameof(request.Name));
        if (request.Scopes is null || request.Scopes.Count == 0)
            throw new ArgumentException("At least one scope is required.");

        var (isValid, unsupported) = PatScopeValidator.Validate(request.Scopes);
        if (!isValid)
            throw new ArgumentException($"Unsupported scope(s): {string.Join(", ", unsupported)}");

        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var base64 = Convert.ToBase64String(rawBytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var plaintext = $"mm_pat_{base64}";

        var (hash, prefix) = hasher.HashToken(plaintext);

        var token = PersonalAccessToken.Create(
            currentUserService.UserId,
            request.Name,
            request.Scopes,
            hash,
            prefix);

        await repository.AddAsync(token, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PatCreatedResponse(
            token.Id,
            token.Name,
            token.Scopes,
            token.CreatedAt,
            plaintext);
    }
}
