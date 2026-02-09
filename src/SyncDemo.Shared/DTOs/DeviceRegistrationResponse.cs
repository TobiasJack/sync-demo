using SyncDemo.Shared.Models;

namespace SyncDemo.Shared.DTOs;

/// <summary>
/// Response for device registration
/// </summary>
public class DeviceRegistrationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Device? Device { get; set; }
    public List<DevicePermission> Permissions { get; set; } = new();
}
