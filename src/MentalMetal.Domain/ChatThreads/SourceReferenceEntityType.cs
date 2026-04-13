namespace MentalMetal.Domain.ChatThreads;

public enum SourceReferenceEntityType
{
    Capture,
    Commitment,
    Delegation,
    LivingBriefDecision,
    LivingBriefRisk,
    LivingBriefRequirements,
    LivingBriefDesignDirection,
    Initiative,
    Person,
    // Forward-compatible reservations for the people-lens capability. Persisted as their string
    // names; safe to add now since they widen the enum without affecting existing rows.
    Observation,
    Goal,
    OneOnOne
}
