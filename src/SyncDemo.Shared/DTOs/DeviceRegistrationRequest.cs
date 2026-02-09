namespace SyncDemo.Shared.DTOs;

/// <summary>
/// Request for device registration
/// </summary>
public class DeviceRegistrationRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
