using MentalMetal.Domain.Interviews;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Interviews;

public sealed class GetUserInterviewsHandler(
    IInterviewRepository repository,
    ICurrentUserService currentUserService)
{
    public async Task<IReadOnlyList<InterviewResponse>> HandleAsync(
        Guid? candidatePersonId,
        InterviewStage? stage,
        CancellationToken cancellationToken)
    {
        var interviews = await repository.GetAllAsync(
            currentUserService.UserId,
            candidatePersonId,
            stage,
            cancellationToken);

        return interviews.Select(InterviewResponse.From).ToList();
    }
}
