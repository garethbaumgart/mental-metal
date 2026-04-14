namespace MentalMetal.Application.Nudges;

/// <summary>
/// Raised when a nudge references a Person or Initiative that does not exist for the current user.
/// Mapped by the Web layer to HTTP 400 with code <c>nudge.linkedEntityNotFound</c>.
/// </summary>
public sealed class LinkedEntityNotFoundException : Exception
{
    public LinkedEntityNotFoundException(string message) : base(message) { }
}
