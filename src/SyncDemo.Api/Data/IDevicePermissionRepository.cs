using SyncDemo.Shared.Models;

namespace SyncDemo.Api.Data;

/// <summary>
/// Repository interface for DevicePermission operations
/// </summary>
public interface IDevicePermissionRepository
{
    Task<List<DevicePermission>> GetPermissionsForDeviceAsync(string deviceId);
    Task<bool> HasPermissionAsync(string deviceId, string entityType, int? entityId, string permissionType);
    Task GrantPermissionAsync(DevicePermission permission);
    Task RevokePermissionAsync(int permissionId);
    Task<List<string>> GetAuthorizedDevicesForEntityAsync(string entityType, int entityId);
}
