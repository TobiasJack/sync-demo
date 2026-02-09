namespace SyncDemo.Shared.Models;

/// <summary>
/// Represents a device permission for accessing specific entities
/// </summary>
public class DevicePermission
{
    public int PermissionId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string PermissionType { get; set; } = string.Empty;
    public DateTime GrantedAt { get; set; }
    public int? GrantedBy { get; set; }
}
