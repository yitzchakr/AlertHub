namespace AlertHub.Domain.Exceptions;

/// <summary>
/// The request is valid in itself but collides with existing state.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
