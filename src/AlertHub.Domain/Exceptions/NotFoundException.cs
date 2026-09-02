namespace AlertHub.Domain.Exceptions;

/// <summary>
/// A referenced entity does not exist, or exists outside the current tenant.
/// The two are deliberately indistinguishable to the caller: revealing that an id
/// is real but belongs to someone else would leak across the tenant boundary.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public static NotFoundException For<T>(Guid id) =>
        new($"{typeof(T).Name} '{id}' was not found.");
}
