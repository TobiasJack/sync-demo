using Dapper;
using SyncDemo.Shared.Models;

namespace SyncDemo.Api.Data;

public interface ISyncItemRepository
{
    Task<IEnumerable<SyncItem>> GetAllAsync();
    Task<SyncItem?> GetByIdAsync(Guid id);
    Task<SyncItem> CreateAsync(SyncItem item);
    Task<bool> UpdateAsync(SyncItem item);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<SyncItem>> GetModifiedSinceAsync(DateTime since);
}

public class SyncItemRepository : ISyncItemRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SyncItemRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<SyncItem>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, Name, Description, CreatedAt, ModifiedAt, IsDeleted, Version 
            FROM SyncItems 
            WHERE IsDeleted = 0 
            ORDER BY ModifiedAt DESC";
        
        return await connection.QueryAsync<SyncItem>(sql);
    }

    public async Task<SyncItem?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, Name, Description, CreatedAt, ModifiedAt, IsDeleted, Version 
            FROM SyncItems 
            WHERE Id = :Id";
        
        return await connection.QueryFirstOrDefaultAsync<SyncItem>(sql, new { Id = id.ToString() });
    }

    public async Task<SyncItem> CreateAsync(SyncItem item)
    {
        using var connection = _connectionFactory.CreateConnection();
        item.Id = Guid.NewGuid();
        item.CreatedAt = DateTime.UtcNow;
        item.ModifiedAt = DateTime.UtcNow;
        item.Version = 1;
        
        const string sql = @"
            INSERT INTO SyncItems (Id, Name, Description, CreatedAt, ModifiedAt, IsDeleted, Version)
            VALUES (:Id, :Name, :Description, :CreatedAt, :ModifiedAt, :IsDeleted, :Version)";
        
        await connection.ExecuteAsync(sql, new
        {
            Id = item.Id.ToString(),
            item.Name,
            item.Description,
            item.CreatedAt,
            item.ModifiedAt,
            IsDeleted = item.IsDeleted ? 1 : 0,
            item.Version
        });
        
        return item;
    }

    public async Task<bool> UpdateAsync(SyncItem item)
    {
        using var connection = _connectionFactory.CreateConnection();
        item.ModifiedAt = DateTime.UtcNow;
        item.Version++;
        
        const string sql = @"
            UPDATE SyncItems 
            SET Name = :Name, 
                Description = :Description, 
                ModifiedAt = :ModifiedAt, 
                IsDeleted = :IsDeleted, 
                Version = :Version 
            WHERE Id = :Id";
        
        var affected = await connection.ExecuteAsync(sql, new
        {
            Id = item.Id.ToString(),
            item.Name,
            item.Description,
            item.ModifiedAt,
            IsDeleted = item.IsDeleted ? 1 : 0,
            item.Version
        });
        
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE SyncItems 
            SET IsDeleted = 1, 
                ModifiedAt = :ModifiedAt 
            WHERE Id = :Id";
        
        var affected = await connection.ExecuteAsync(sql, new
        {
            Id = id.ToString(),
            ModifiedAt = DateTime.UtcNow
        });
        
        return affected > 0;
    }

    public async Task<IEnumerable<SyncItem>> GetModifiedSinceAsync(DateTime since)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, Name, Description, CreatedAt, ModifiedAt, IsDeleted, Version 
            FROM SyncItems 
            WHERE ModifiedAt > :Since 
            ORDER BY ModifiedAt DESC";
        
        return await connection.QueryAsync<SyncItem>(sql, new { Since = since });
    }
}
