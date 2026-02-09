namespace SyncDemo.Shared.Models;

/// <summary>
/// Represents a sync operation message
/// </summary>
public class SyncMessage
{
    public string Operation { get; set; } = string.Empty; // CREATE, UPDATE, DELETE
    public SyncItem Item { get; set; } = new();
    public DateTime Timestamp { get; set; }
}
