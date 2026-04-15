using MentalMetal.Application.Common;
using MentalMetal.Domain.Goals;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Goals;

internal static class GoalPersonNameResolver
{
    public static async Task<string?> ResolveAsync(
        IPersonRepository personRepository,
        Guid userId,
        Guid personId,
        CancellationToken cancellationToken)
    {
        var people = await personRepository.GetByIdsAsync(userId, [personId], cancellationToken);
        return people.FirstOrDefault()?.Name;
    }
}

public sealed class CreateGoalHandler(
    IGoalRepository repository,
    IPersonRepository personRepository,
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
        var personName = await GoalPersonNameResolver.ResolveAsync(
            personRepository, currentUserService.UserId, goal.PersonId, cancellationToken);
        return GoalResponse.From(goal, personName);
    }
}

public sealed class UpdateGoalHandler(
    IGoalRepository repository,
    IPersonRepository personRepository,
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
        var personName = await GoalPersonNameResolver.ResolveAsync(
            personRepository, currentUserService.UserId, goal.PersonId, cancellationToken);
        return GoalResponse.From(goal, personName);
    }
}

public sealed class GetGoalByIdHandler(
    IGoalRepository repository,
    IPersonRepository personRepository,
    ICurrentUserService currentUserService)
{
    public async Task<GoalResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var goal = await repository.GetByIdAsync(id, cancellationToken);
        if (goal is null || goal.UserId != currentUserService.UserId)
            return null;

        var personName = await GoalPersonNameResolver.ResolveAsync(
            personRepository, currentUserService.UserId, goal.PersonId, cancellationToken);
        return GoalResponse.From(goal, personName);
    }
}

public sealed class GetUserGoalsHandler(
    IGoalRepository repository,
    IPersonRepository personRepository,
    ICurrentUserService currentUserService)
{
    public async Task<List<GoalResponse>> HandleAsync(
        Guid? personIdFilter,
        GoalType? typeFilter,
        GoalStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var items = await repository.GetAllAsync(
            userId, personIdFilter, typeFilter, statusFilter, null, null, cancellationToken);

        // Batch-resolve person names to avoid N+1.
        var personIds = items.Select(g => g.PersonId).Distinct().ToList();
        var people = await personRepository.GetByIdsAsync(userId, personIds, cancellationToken);
        var personNames = people.ToDictionary(p => p.Id, p => p.Name);

        return items
            .Select(g => GoalResponse.From(
                g, personNames.TryGetValue(g.PersonId, out var pn) ? pn : null))
            .ToList();
    }
}

public sealed class AchieveGoalHandler(
    IGoalRepository repository,
    IPersonRepository personRepository,
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
        var personName = await GoalPersonNameResolver.ResolveAsync(
            personRepository, currentUserService.UserId, goal.PersonId, cancellationToken);
        return GoalResponse.From(goal, personName);
    }
}

public sealed class MissGoalHandler(
    IGoalRepository repository,
    IPersonRepository personRepository,
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
        var personName = await GoalPersonNameResolver.ResolveAsync(
            personRepository, currentUserService.UserId, goal.PersonId, cancellationToken);
        return GoalResponse.From(goal, personName);
    }
}

public sealed class DeferGoalHandler(
    IGoalRepository repository,
    IPersonRepository personRepository,
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
        var personName = await GoalPersonNameResolver.ResolveAsync(
            personRepository, currentUserService.UserId, goal.PersonId, cancellationToken);
        return GoalResponse.From(goal, personName);
    }
}

public sealed class ReactivateGoalHandler(
    IGoalRepository repository,
    IPersonRepository personRepository,
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
        var personName = await GoalPersonNameResolver.ResolveAsync(
            personRepository, currentUserService.UserId, goal.PersonId, cancellationToken);
        return GoalResponse.From(goal, personName);
    }
}

public sealed class RecordGoalCheckInHandler(
    IGoalRepository repository,
    IPersonRepository personRepository,
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
        var personName = await GoalPersonNameResolver.ResolveAsync(
            personRepository, currentUserService.UserId, goal.PersonId, cancellationToken);
        return GoalResponse.From(goal, personName);
    }
}
