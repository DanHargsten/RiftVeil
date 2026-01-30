namespace RiftVeil.Domain.Entities;

/// <summary>
/// Lightweight entity to verify EF wiring in local setups.
/// </summary>
public class DbSmokeTest
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
