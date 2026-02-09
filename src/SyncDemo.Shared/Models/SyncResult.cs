namespace SyncDemo.Shared.Models;

/// <summary>
/// Represents the result of a sync operation
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ItemsProcessed { get; set; }
    public List<SyncItem> Items { get; set; } = new();
}
