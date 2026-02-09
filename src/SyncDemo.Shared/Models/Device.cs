namespace SyncDemo.Shared.Models;

/// <summary>
/// Represents a registered device in the system
/// </summary>
public class Device
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public DateTime LastSeen { get; set; }
    public bool IsActive { get; set; }
}
