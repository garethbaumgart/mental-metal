using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.People;

public sealed class CandidateDetails : ValueObject
{
    private static readonly HashSet<PipelineStatus> TerminalStatuses =
        [PipelineStatus.Hired, PipelineStatus.Rejected, PipelineStatus.Withdrawn];

    private static readonly Dictionary<PipelineStatus, PipelineStatus> ForwardTransitions = new()
    {
        [PipelineStatus.New] = PipelineStatus.Screening,
        [PipelineStatus.Screening] = PipelineStatus.Interviewing,
        [PipelineStatus.Interviewing] = PipelineStatus.OfferStage,
        [PipelineStatus.OfferStage] = PipelineStatus.Hired
    };

    public PipelineStatus PipelineStatus { get; private set; }
    public string? CvNotes { get; private set; }
    public string? SourceChannel { get; private set; }

    private CandidateDetails() { } // EF Core

    public static CandidateDetails Create(
        PipelineStatus pipelineStatus = PipelineStatus.New,
        string? cvNotes = null,
        string? sourceChannel = null)
    {
        return new CandidateDetails
        {
            PipelineStatus = pipelineStatus,
            CvNotes = cvNotes?.Trim(),
            SourceChannel = sourceChannel?.Trim()
        };
    }

    public static void ValidateTransition(PipelineStatus current, PipelineStatus target)
    {
        if (TerminalStatuses.Contains(current))
            throw new ArgumentException(
                $"Cannot transition from terminal state '{current}'.");

        // Rejected and Withdrawn are reachable from any non-terminal state
        if (target is PipelineStatus.Rejected or PipelineStatus.Withdrawn)
            return;

        if (!ForwardTransitions.TryGetValue(current, out var validNext) || validNext != target)
            throw new ArgumentException(
                $"Invalid pipeline transition from '{current}' to '{target}'.");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PipelineStatus;
        yield return CvNotes;
        yield return SourceChannel;
    }
}
