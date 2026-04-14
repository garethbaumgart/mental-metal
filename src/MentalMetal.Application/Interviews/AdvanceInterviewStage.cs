using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Interviews;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Interviews;

public sealed class AdvanceInterviewStageHandler(
    IInterviewRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<InterviewResponse> HandleAsync(
        Guid id, AdvanceInterviewStageRequest request, CancellationToken cancellationToken)
    {
        var interview = await repository.GetByIdAsync(id, cancellationToken);
        if (interview is null || interview.UserId != currentUserService.UserId)
            throw new NotFoundException("Interview", id);

        interview.AdvanceStage(request.TargetStage, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return InterviewResponse.From(interview);
    }
}
