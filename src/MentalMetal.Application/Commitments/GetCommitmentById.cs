using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Commitments;

public sealed class GetCommitmentByIdHandler(
    ICommitmentRepository commitmentRepository,
    ICurrentUserService currentUserService)
{
    public async Task<CommitmentResponse?> HandleAsync(
        Guid commitmentId, CancellationToken cancellationToken)
    {
        var commitment = await commitmentRepository.GetByIdAsync(commitmentId, cancellationToken);

        if (commitment is null || commitment.UserId != currentUserService.UserId)
            return null;

        return CommitmentResponse.From(commitment);
    }
}
