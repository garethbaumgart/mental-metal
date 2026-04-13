namespace MentalMetal.Domain.Common;

/// <summary>
/// Thrown when a requested aggregate or entity is not found.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, Guid id)
        : base($"{entityName} '{id}' not found.")
    {
        EntityName = entityName;
        EntityId = id;
    }

    public NotFoundException(string message) : base(message) { }

    public string? EntityName { get; }
    public Guid? EntityId { get; }
}
