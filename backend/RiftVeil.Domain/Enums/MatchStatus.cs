namespace RiftVeil.Domain.Enums
{
    /// <summary>
    /// Aligns match state with scheduling and UI workflows.
    /// </summary>
    public enum MatchStatus
    {
        Scheduled = 0,
        Live = 1,
        Finished = 2,
        Cancelled = 3
    }
}
