using SyncDemo.Shared.Models;

namespace SyncDemo.Api.Data;

/// <summary>
/// Repository interface for Device operations
/// </summary>
public interface IDeviceRepository
{
    Task<Device?> GetByIdAsync(string deviceId);
    Task<Device> RegisterDeviceAsync(Device device);
    Task UpdateLastSeenAsync(string deviceId);
    Task<List<Device>> GetDevicesByUserIdAsync(int userId);
    Task<bool> IsDeviceActiveAsync(string deviceId);
}
