namespace MentalMetal.Application.MyQueue;

/// <summary>
/// Configuration knobs for the My Queue feature. All values are in days.
/// </summary>
public sealed class MyQueueOptions
{
    public const string SectionName = "MyQueue";

    /// <summary>
    /// Commitments due within this number of calendar days qualify for the queue. Default 7.
    /// </summary>
    public int CommitmentDueSoonDays { get; set; } = 7;

    /// <summary>
    /// Delegations whose last-touch (LastFollowedUpAt ?? CreatedAt) is at least this many days
    /// ago qualify for a staleness bump. Default 7.
    /// </summary>
    public int DelegationStalenessDays { get; set; } = 7;

    /// <summary>
    /// Captures older than this many days qualify for the queue. Default 3.
    /// </summary>
    public int CaptureStalenessDays { get; set; } = 3;

    /// <summary>
    /// Upper bound on the number of candidate rows fetched per item type per request.
    /// Prevents unbounded in-memory scoring on pathological data. Default 200.
    /// </summary>
    public int CandidateFetchLimit { get; set; } = 200;
}
