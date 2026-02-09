namespace SyncDemo.Shared.Models;

/// <summary>
/// Represents a synchronizable item in the system
/// </summary>
public class SyncItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public bool IsDeleted { get; set; }
    public int Version { get; set; }
}
