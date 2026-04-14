namespace MentalMetal.Application.Briefings;

public sealed record GenerateMorningBriefingCommand(bool Force);

public sealed record GenerateBriefingResult(BriefingResponse Briefing, bool WasCached);

public sealed class GenerateMorningBriefingHandler(BriefingService briefingService)
{
    public async Task<GenerateBriefingResult> HandleAsync(
        GenerateMorningBriefingCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var result = await briefingService.GenerateMorningAsync(command.Force, cancellationToken);
        return new GenerateBriefingResult(result.Briefing.ToResponse(), result.WasCached);
    }
}
