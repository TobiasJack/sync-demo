namespace SyncDemo.Api.Infrastructure.SignalR;

public interface IConnectionManager
{
    Task<bool> IsDeviceOnlineAsync(string deviceId);
    Task<string?> GetConnectionIdAsync(string deviceId);
    Task AddConnectionAsync(string deviceId, string connectionId);
    Task RemoveConnectionAsync(string connectionId);
}
