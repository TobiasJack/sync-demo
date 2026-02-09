using Dapper;
using SyncDemo.Shared.Models;

namespace SyncDemo.Api.Data;

/// <summary>
/// Repository for Device operations
/// </summary>
public class DeviceRepository : IDeviceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DeviceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Device?> GetByIdAsync(string deviceId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"SELECT DEVICE_ID as DeviceId, DEVICE_NAME as DeviceName, USER_ID as UserId, 
                           DEVICE_TYPE as DeviceType, REGISTERED_AT as RegisteredAt, 
                           LAST_SEEN as LastSeen, IS_ACTIVE as IsActive 
                           FROM DEVICES WHERE DEVICE_ID = :DeviceId";
        return await connection.QueryFirstOrDefaultAsync<Device>(sql, new { DeviceId = deviceId });
    }

    public async Task<Device> RegisterDeviceAsync(Device device)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"INSERT INTO DEVICES (DEVICE_ID, DEVICE_NAME, USER_ID, DEVICE_TYPE, REGISTERED_AT, LAST_SEEN, IS_ACTIVE)
                           VALUES (:DeviceId, :DeviceName, :UserId, :DeviceType, SYSTIMESTAMP, SYSTIMESTAMP, 1)";
        
        await connection.ExecuteAsync(sql, new
        {
            DeviceId = device.DeviceId,
            DeviceName = device.DeviceName,
            UserId = device.UserId,
            DeviceType = device.DeviceType
        });
        
        device.RegisteredAt = DateTime.UtcNow;
        device.LastSeen = DateTime.UtcNow;
        device.IsActive = true;
        
        return device;
    }

    public async Task UpdateLastSeenAsync(string deviceId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "UPDATE DEVICES SET LAST_SEEN = SYSTIMESTAMP WHERE DEVICE_ID = :DeviceId";
        await connection.ExecuteAsync(sql, new { DeviceId = deviceId });
    }

    public async Task<List<Device>> GetDevicesByUserIdAsync(int userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"SELECT DEVICE_ID as DeviceId, DEVICE_NAME as DeviceName, USER_ID as UserId, 
                           DEVICE_TYPE as DeviceType, REGISTERED_AT as RegisteredAt, 
                           LAST_SEEN as LastSeen, IS_ACTIVE as IsActive 
                           FROM DEVICES WHERE USER_ID = :UserId AND IS_ACTIVE = 1";
        var result = await connection.QueryAsync<Device>(sql, new { UserId = userId });
        return result.ToList();
    }

    public async Task<bool> IsDeviceActiveAsync(string deviceId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT IS_ACTIVE FROM DEVICES WHERE DEVICE_ID = :DeviceId";
        var isActive = await connection.QueryFirstOrDefaultAsync<int?>(sql, new { DeviceId = deviceId });
        return isActive == 1;
    }
}
