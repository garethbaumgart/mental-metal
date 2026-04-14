namespace MentalMetal.Domain.Common;

/// <summary>
/// Thrown when an aggregate rejects an operation because it would violate a domain
/// invariant (e.g. an invalid stage transition). Web endpoints map this to HTTP 409.
/// </summary>
public sealed class DomainException : Exception
{
    public string? Code { get; }

    public DomainException(string message) : base(message) { }

    public DomainException(string message, string code) : base(message)
    {
        Code = code;
    }
}
