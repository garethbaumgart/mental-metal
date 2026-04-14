using MentalMetal.Domain.Interviews;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Interviews;

public sealed class GetInterviewByIdHandler(
    IInterviewRepository repository,
    ICurrentUserService currentUserService)
{
    public async Task<InterviewResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var interview = await repository.GetByIdAsync(id, cancellationToken);
        if (interview is null || interview.UserId != currentUserService.UserId)
            return null;

        return InterviewResponse.From(interview);
    }
}
