using SyncDemo.Api.Data;

namespace SyncDemo.Api.Services;

/// <summary>
/// Service for handling device permission checks
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IDevicePermissionRepository _permissionRepo;
    private readonly IDeviceRepository _deviceRepo;

    public PermissionService(
        IDevicePermissionRepository permissionRepo,
        IDeviceRepository deviceRepo)
    {
        _permissionRepo = permissionRepo;
        _deviceRepo = deviceRepo;
    }

    public async Task<bool> CanDeviceAccessEntityAsync(string deviceId, string entityType, int? entityId)
    {
        // Check if device is active
        if (!await _deviceRepo.IsDeviceActiveAsync(deviceId))
            return false;

        // Check permission
        return await _permissionRepo.HasPermissionAsync(deviceId, entityType, entityId, "READ");
    }

    public async Task<List<int>> GetAccessibleEntityIdsAsync(string deviceId, string entityType)
    {
        var permissions = await _permissionRepo.GetPermissionsForDeviceAsync(deviceId);
        
        // If device has access to ALL entities
        if (permissions.Any(p => p.EntityType == entityType && p.EntityId == null) ||
            permissions.Any(p => p.EntityType == "ALL"))
        {
            return new List<int>(); // Empty list = All allowed
        }
        
        // Otherwise only specific entity IDs
        return permissions
            .Where(p => p.EntityType == entityType && p.EntityId.HasValue)
            .Select(p => p.EntityId!.Value)
            .ToList();
    }
}
