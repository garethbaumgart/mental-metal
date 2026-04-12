using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Commitments;

public sealed class GetUserCommitmentsHandler(
    ICommitmentRepository commitmentRepository,
    ICurrentUserService currentUserService)
{
    public async Task<List<CommitmentResponse>> HandleAsync(
        CommitmentDirection? directionFilter,
        CommitmentStatus? statusFilter,
        Guid? personIdFilter,
        Guid? initiativeIdFilter,
        bool? overdueFilter,
        CancellationToken cancellationToken)
    {
        var commitments = await commitmentRepository.GetAllAsync(
            currentUserService.UserId,
            directionFilter,
            statusFilter,
            personIdFilter,
            initiativeIdFilter,
            overdueFilter,
            cancellationToken);

        return commitments.Select(CommitmentResponse.From).ToList();
    }
}
