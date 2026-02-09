using System.Collections.Concurrent;

namespace SyncDemo.Api.Infrastructure.SignalR;

public class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, string> _deviceConnections = new();
    private readonly ConcurrentDictionary<string, string> _connectionDevices = new();

    public Task<bool> IsDeviceOnlineAsync(string deviceId)
    {
        return Task.FromResult(_deviceConnections.ContainsKey(deviceId));
    }

    public Task<string?> GetConnectionIdAsync(string deviceId)
    {
        _deviceConnections.TryGetValue(deviceId, out var connectionId);
        return Task.FromResult(connectionId);
    }

    public Task AddConnectionAsync(string deviceId, string connectionId)
    {
        _deviceConnections[deviceId] = connectionId;
        _connectionDevices[connectionId] = deviceId;
        return Task.CompletedTask;
    }

    public Task RemoveConnectionAsync(string connectionId)
    {
        if (_connectionDevices.TryRemove(connectionId, out var deviceId))
        {
            _deviceConnections.TryRemove(deviceId, out _);
        }
        return Task.CompletedTask;
    }
}
