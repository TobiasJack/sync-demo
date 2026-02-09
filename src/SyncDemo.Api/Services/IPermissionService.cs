namespace SyncDemo.Api.Services;

/// <summary>
/// Service interface for permission checks
/// </summary>
public interface IPermissionService
{
    Task<bool> CanDeviceAccessEntityAsync(string deviceId, string entityType, int? entityId);
    Task<List<int>> GetAccessibleEntityIdsAsync(string deviceId, string entityType);
}
