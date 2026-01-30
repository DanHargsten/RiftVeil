namespace RiftVeil.Domain.Common;

/// <summary>
/// Centralizes identity and timestamps to keep persistence consistent.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; internal set; }
    public DateTimeOffset? UpdatedAtUtc { get; internal set; }
}
