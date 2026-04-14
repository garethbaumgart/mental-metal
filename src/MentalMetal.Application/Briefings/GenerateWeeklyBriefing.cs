namespace MentalMetal.Application.Briefings;

public sealed record GenerateWeeklyBriefingCommand(bool Force);

public sealed class GenerateWeeklyBriefingHandler(BriefingService briefingService)
{
    public async Task<GenerateBriefingResult> HandleAsync(
        GenerateWeeklyBriefingCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var result = await briefingService.GenerateWeeklyAsync(command.Force, cancellationToken);
        return new GenerateBriefingResult(result.Briefing.ToResponse(), result.WasCached);
    }
}
