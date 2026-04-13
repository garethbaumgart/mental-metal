namespace MentalMetal.Domain.Initiatives.LivingBrief;

public sealed class LivingBrief
{
    private readonly List<KeyDecision> _keyDecisions = [];
    private readonly List<Risk> _risks = [];
    private readonly List<RequirementsSnapshot> _requirementsHistory = [];
    private readonly List<DesignDirectionSnapshot> _designDirectionHistory = [];

    public string Summary { get; private set; } = string.Empty;
    public DateTimeOffset? SummaryLastRefreshedAt { get; private set; }
    public int BriefVersion { get; private set; }
    public BriefSource SummarySource { get; private set; } = BriefSource.Manual;
    public IReadOnlyList<Guid> SummarySourceCaptureIds { get; private set; } = [];

    public IReadOnlyList<KeyDecision> KeyDecisions => _keyDecisions;
    public IReadOnlyList<Risk> Risks => _risks;
    public IReadOnlyList<RequirementsSnapshot> RequirementsHistory => _requirementsHistory;
    public IReadOnlyList<DesignDirectionSnapshot> DesignDirectionHistory => _designDirectionHistory;

    private LivingBrief() { }

    public static LivingBrief Empty() => new();

    // Mutators are package-internal — callers must go through Initiative aggregate methods.
    internal void SetSummary(string summary, BriefSource source, IReadOnlyList<Guid> sourceCaptureIds, DateTimeOffset now)
    {
        Summary = summary ?? string.Empty;
        SummarySource = source;
        SummarySourceCaptureIds = sourceCaptureIds?.ToList() ?? [];
        SummaryLastRefreshedAt = now;
        BriefVersion++;
    }

    internal KeyDecision AppendDecision(string description, string? rationale, BriefSource source, IReadOnlyList<Guid> sourceCaptureIds, DateTimeOffset now)
    {
        var decision = new KeyDecision
        {
            Id = Guid.NewGuid(),
            Description = description,
            Rationale = rationale,
            Source = source,
            SourceCaptureIds = sourceCaptureIds?.ToList() ?? [],
            LoggedAt = now
        };
        _keyDecisions.Add(decision);
        BriefVersion++;
        return decision;
    }

    internal Risk AppendRisk(string description, RiskSeverity severity, BriefSource source, IReadOnlyList<Guid> sourceCaptureIds, DateTimeOffset now)
    {
        var risk = new Risk
        {
            Id = Guid.NewGuid(),
            Description = description,
            Severity = severity,
            Status = RiskStatus.Open,
            Source = source,
            SourceCaptureIds = sourceCaptureIds?.ToList() ?? [],
            RaisedAt = now
        };
        _risks.Add(risk);
        BriefVersion++;
        return risk;
    }

    internal Risk ResolveRiskById(Guid riskId, string? resolutionNote, DateTimeOffset now)
    {
        var idx = _risks.FindIndex(r => r.Id == riskId);
        if (idx < 0)
            throw new ArgumentException($"Risk '{riskId}' not found.", nameof(riskId));

        var existing = _risks[idx];
        if (existing.Status == RiskStatus.Resolved)
            throw new InvalidOperationException($"Risk '{riskId}' is already resolved.");

        var resolved = existing.Resolve(now, resolutionNote);
        _risks[idx] = resolved;
        BriefVersion++;
        return resolved;
    }

    internal RequirementsSnapshot AppendRequirements(string content, BriefSource source, IReadOnlyList<Guid> sourceCaptureIds, DateTimeOffset now)
    {
        var snap = new RequirementsSnapshot
        {
            Id = Guid.NewGuid(),
            Content = content,
            Source = source,
            SourceCaptureIds = sourceCaptureIds?.ToList() ?? [],
            CapturedAt = now
        };
        _requirementsHistory.Add(snap);
        BriefVersion++;
        return snap;
    }

    internal DesignDirectionSnapshot AppendDesignDirection(string content, BriefSource source, IReadOnlyList<Guid> sourceCaptureIds, DateTimeOffset now)
    {
        var snap = new DesignDirectionSnapshot
        {
            Id = Guid.NewGuid(),
            Content = content,
            Source = source,
            SourceCaptureIds = sourceCaptureIds?.ToList() ?? [],
            CapturedAt = now
        };
        _designDirectionHistory.Add(snap);
        BriefVersion++;
        return snap;
    }
}
