namespace MentalMetal.Application.Briefings;

public sealed record GenerateOneOnOnePrepCommand(Guid PersonId, bool Force);

public sealed class GenerateOneOnOnePrepHandler(BriefingService briefingService)
{
    /// <summary>
    /// Returns null when the person does not exist or does not belong to the user;
    /// the endpoint maps that to HTTP 404.
    /// </summary>
    public async Task<GenerateBriefingResult?> HandleAsync(
        GenerateOneOnOnePrepCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var result = await briefingService.GenerateOneOnOnePrepAsync(command.PersonId, command.Force, cancellationToken);
        return result is null
            ? null
            : new GenerateBriefingResult(result.Briefing.ToResponse(), result.WasCached);
    }
}
