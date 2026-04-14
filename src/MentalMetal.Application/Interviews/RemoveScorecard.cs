using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Interviews;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Interviews;

public sealed class RemoveInterviewScorecardHandler(
    IInterviewRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task HandleAsync(
        Guid interviewId, Guid scorecardId, CancellationToken cancellationToken)
    {
        var interview = await repository.GetByIdAsync(interviewId, cancellationToken);
        if (interview is null || interview.UserId != currentUserService.UserId)
            throw new NotFoundException("Interview", interviewId);

        if (!interview.Scorecards.Any(s => s.Id == scorecardId))
            throw new ScorecardNotFoundException($"Scorecard '{scorecardId}' not found on interview '{interviewId}'.");

        var removed = interview.RemoveScorecard(scorecardId, timeProvider.GetUtcNow());
        if (removed is not null)
            repository.MarkOwnedRemoved(removed);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
