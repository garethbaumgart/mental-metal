using MentalMetal.Application.Common;
using MentalMetal.Domain.Goals;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Goals;

public sealed class CreateGoalHandler(
    IGoalRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<GoalResponse> HandleAsync(
        CreateGoalRequest request, CancellationToken cancellationToken)
    {
        var goal = Goal.Create(
            currentUserService.UserId,
            request.PersonId,
            request.Title,
            request.GoalType,
            request.Description,
            request.TargetDate);

        await repository.AddAsync(goal, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GoalResponse.From(goal);
    }
}

public sealed class UpdateGoalHandler(
    IGoalRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<GoalResponse> HandleAsync(
        Guid id, UpdateGoalRequest request, CancellationToken cancellationToken)
    {
        var goal = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Goal not found.");

        if (goal.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Goal not found.");

        goal.Update(request.Title, request.Description, request.TargetDate);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GoalResponse.From(goal);
    }
}

public sealed class GetGoalByIdHandler(
    IGoalRepository repository,
    ICurrentUserService currentUserService)
{
    public async Task<GoalResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var goal = await repository.GetByIdAsync(id, cancellationToken);
        if (goal is null || goal.UserId != currentUserService.UserId)
            return null;

        return GoalResponse.From(goal);
    }
}

public sealed class GetUserGoalsHandler(
    IGoalRepository repository,
    ICurrentUserService currentUserService)
{
    public async Task<List<GoalResponse>> HandleAsync(
        Guid? personIdFilter,
        GoalType? typeFilter,
        GoalStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        var items = await repository.GetAllAsync(
            currentUserService.UserId, personIdFilter, typeFilter, statusFilter, null, null, cancellationToken);
        return items.Select(GoalResponse.From).ToList();
    }
}

public sealed class AchieveGoalHandler(
    IGoalRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<GoalResponse> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var goal = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Goal not found.");

        if (goal.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Goal not found.");

        goal.Achieve();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GoalResponse.From(goal);
    }
}

public sealed class MissGoalHandler(
    IGoalRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<GoalResponse> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var goal = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Goal not found.");

        if (goal.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Goal not found.");

        goal.Miss();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GoalResponse.From(goal);
    }
}

public sealed class DeferGoalHandler(
    IGoalRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<GoalResponse> HandleAsync(
        Guid id, DeferGoalRequest request, CancellationToken cancellationToken)
    {
        var goal = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Goal not found.");

        if (goal.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Goal not found.");

        goal.Defer(request.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GoalResponse.From(goal);
    }
}

public sealed class ReactivateGoalHandler(
    IGoalRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<GoalResponse> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var goal = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Goal not found.");

        if (goal.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Goal not found.");

        goal.Reactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GoalResponse.From(goal);
    }
}

public sealed class RecordGoalCheckInHandler(
    IGoalRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<GoalResponse> HandleAsync(
        Guid id, RecordCheckInRequest request, CancellationToken cancellationToken)
    {
        var goal = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Goal not found.");

        if (goal.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Goal not found.");

        var checkIn = goal.RecordCheckIn(request.Note, request.Progress);
        repository.MarkOwnedAdded(checkIn);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GoalResponse.From(goal);
    }
}
