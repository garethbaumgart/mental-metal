using MentalMetal.Application.Common;
using MentalMetal.Domain.Interviews;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Interviews;

public sealed class CreateInterviewHandler(
    IInterviewRepository interviewRepository,
    IPersonRepository personRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<InterviewResponse> HandleAsync(
        CreateInterviewRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var person = await personRepository.GetByIdAsync(request.CandidatePersonId, cancellationToken);
        if (person is null || person.UserId != userId)
            throw new CandidateNotFoundException($"Candidate person '{request.CandidatePersonId}' not found.");

        if (person.Type != PersonType.Candidate)
            throw new ArgumentException("Person is not a candidate.", nameof(request.CandidatePersonId));

        var now = timeProvider.GetUtcNow();
        var interview = Interview.Create(
            userId,
            request.CandidatePersonId,
            request.RoleTitle,
            now,
            request.ScheduledAtUtc);

        await interviewRepository.AddAsync(interview, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return InterviewResponse.From(interview);
    }
}
