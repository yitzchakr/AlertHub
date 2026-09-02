using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AlertHub.Infrastructure.Persistence.Converters;

/// <summary>
/// Stamps DateTimeKind.Utc on read. Identity on write and the same store type, so it
/// changes no columns and generates no migration.
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
