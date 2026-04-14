using MentalMetal.Application.Common;
using MentalMetal.Domain.Observations;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Observations;

public sealed class CreateObservationHandler(
    IObservationRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<ObservationResponse> HandleAsync(
        CreateObservationRequest request, CancellationToken cancellationToken)
    {
        var observation = Observation.Create(
            currentUserService.UserId,
            request.PersonId,
            request.Description,
            request.Tag,
            request.OccurredAt,
            request.SourceCaptureId);

        await repository.AddAsync(observation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ObservationResponse.From(observation);
    }
}

public sealed class UpdateObservationHandler(
    IObservationRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<ObservationResponse> HandleAsync(
        Guid id, UpdateObservationRequest request, CancellationToken cancellationToken)
    {
        var observation = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Observation not found.");

        if (observation.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Observation not found.");

        observation.Update(request.Description, request.Tag);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ObservationResponse.From(observation);
    }
}

public sealed class GetObservationByIdHandler(
    IObservationRepository repository,
    ICurrentUserService currentUserService)
{
    public async Task<ObservationResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var observation = await repository.GetByIdAsync(id, cancellationToken);
        if (observation is null || observation.UserId != currentUserService.UserId)
            return null;

        return ObservationResponse.From(observation);
    }
}

public sealed class GetUserObservationsHandler(
    IObservationRepository repository,
    ICurrentUserService currentUserService)
{
    public async Task<List<ObservationResponse>> HandleAsync(
        Guid? personIdFilter,
        ObservationTag? tagFilter,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var items = await repository.GetAllAsync(
            currentUserService.UserId, personIdFilter, tagFilter, fromDate, toDate, cancellationToken);
        return items.Select(ObservationResponse.From).ToList();
    }
}

public sealed class DeleteObservationHandler(
    IObservationRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<bool> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var observation = await repository.GetByIdAsync(id, cancellationToken);
        if (observation is null || observation.UserId != currentUserService.UserId)
            return false;

        observation.MarkDeleted();
        repository.Remove(observation);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
