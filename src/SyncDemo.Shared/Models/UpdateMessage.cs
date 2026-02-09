namespace SyncDemo.Shared.Models;

/// <summary>
/// Represents an update message for Oracle AQ event-driven architecture
/// </summary>
public class UpdateMessage
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string DataJson { get; set; } = string.Empty;
}
