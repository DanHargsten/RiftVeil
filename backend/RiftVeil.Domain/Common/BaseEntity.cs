namespace RiftVeil.Domain.Common;

/// <summary>
/// Base entity class for all domain entities.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; internal set; }
    public DateTimeOffset? UpdatedAtUtc { get; internal set; }
}
