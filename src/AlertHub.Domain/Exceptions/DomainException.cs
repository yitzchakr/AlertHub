namespace AlertHub.Domain.Exceptions;

/// <summary>
/// A business rule was violated. The caller's request was well-formed but not allowed.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
