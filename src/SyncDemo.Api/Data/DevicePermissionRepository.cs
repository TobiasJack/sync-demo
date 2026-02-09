using Dapper;
using SyncDemo.Shared.Models;

namespace SyncDemo.Api.Data;

/// <summary>
/// Repository for DevicePermission operations
/// </summary>
public class DevicePermissionRepository : IDevicePermissionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DevicePermissionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<DevicePermission>> GetPermissionsForDeviceAsync(string deviceId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"SELECT PERMISSION_ID as PermissionId, DEVICE_ID as DeviceId, 
                           ENTITY_TYPE as EntityType, ENTITY_ID as EntityId, 
                           PERMISSION_TYPE as PermissionType, GRANTED_AT as GrantedAt, 
                           GRANTED_BY as GrantedBy
                           FROM DEVICE_PERMISSIONS 
                           WHERE DEVICE_ID = :DeviceId 
                           ORDER BY ENTITY_TYPE, ENTITY_ID";
        
        var result = await connection.QueryAsync<DevicePermission>(sql, new { DeviceId = deviceId });
        return result.ToList();
    }

    public async Task<bool> HasPermissionAsync(string deviceId, string entityType, int? entityId, string permissionType)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"SELECT COUNT(*) FROM DEVICE_PERMISSIONS 
                           WHERE DEVICE_ID = :DeviceId 
                           AND (ENTITY_TYPE = :EntityType OR ENTITY_TYPE = 'ALL')
                           AND (ENTITY_ID = :EntityId OR ENTITY_ID IS NULL)
                           AND (PERMISSION_TYPE = :PermissionType OR PERMISSION_TYPE = 'ALL')";
        
        var count = await connection.ExecuteScalarAsync<int>(sql, new 
        { 
            DeviceId = deviceId, 
            EntityType = entityType, 
            EntityId = entityId,
            PermissionType = permissionType
        });
        
        return count > 0;
    }

    public async Task GrantPermissionAsync(DevicePermission permission)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"INSERT INTO DEVICE_PERMISSIONS 
                           (DEVICE_ID, ENTITY_TYPE, ENTITY_ID, PERMISSION_TYPE, GRANTED_BY)
                           VALUES (:DeviceId, :EntityType, :EntityId, :PermissionType, :GrantedBy)";
        
        await connection.ExecuteAsync(sql, new
        {
            DeviceId = permission.DeviceId,
            EntityType = permission.EntityType,
            EntityId = permission.EntityId,
            PermissionType = permission.PermissionType,
            GrantedBy = permission.GrantedBy
        });
    }

    public async Task RevokePermissionAsync(int permissionId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM DEVICE_PERMISSIONS WHERE PERMISSION_ID = :PermissionId";
        await connection.ExecuteAsync(sql, new { PermissionId = permissionId });
    }

    public async Task<List<string>> GetAuthorizedDevicesForEntityAsync(string entityType, int entityId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"SELECT DISTINCT DEVICE_ID 
                           FROM DEVICE_PERMISSIONS
                           WHERE (ENTITY_TYPE = :EntityType OR ENTITY_TYPE = 'ALL')
                           AND (ENTITY_ID = :EntityId OR ENTITY_ID IS NULL)
                           AND (PERMISSION_TYPE IN ('READ', 'WRITE', 'ALL'))";
        
        var result = await connection.QueryAsync<string>(sql, new { EntityType = entityType, EntityId = entityId });
        return result.ToList();
    }
}
