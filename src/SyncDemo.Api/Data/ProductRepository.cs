using Dapper;
using SyncDemo.Shared.Models;

namespace SyncDemo.Api.Data;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<int> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
}

public class ProductRepository : IProductRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ProductRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT ID as Id, NAME as Name, DESCRIPTION as Description, 
                   PRICE as Price, STOCK as Stock,
                   CREATED_AT as CreatedAt, UPDATED_AT as UpdatedAt 
            FROM PRODUCTS 
            ORDER BY CREATED_AT DESC";
        
        return await connection.QueryAsync<Product>(sql);
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT ID as Id, NAME as Name, DESCRIPTION as Description, 
                   PRICE as Price, STOCK as Stock,
                   CREATED_AT as CreatedAt, UPDATED_AT as UpdatedAt 
            FROM PRODUCTS 
            WHERE ID = :Id";
        
        return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { Id = id });
    }

    public async Task<int> CreateAsync(Product product)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO PRODUCTS (NAME, DESCRIPTION, PRICE, STOCK)
            VALUES (:Name, :Description, :Price, :Stock)
            RETURNING ID INTO :Id";
        
        var parameters = new DynamicParameters();
        parameters.Add("Name", product.Name);
        parameters.Add("Description", product.Description);
        parameters.Add("Price", product.Price);
        parameters.Add("Stock", product.Stock);
        parameters.Add("Id", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);
        
        await connection.ExecuteAsync(sql, parameters);
        
        return parameters.Get<int>("Id");
    }

    public async Task UpdateAsync(Product product)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE PRODUCTS 
            SET NAME = :Name, 
                DESCRIPTION = :Description, 
                PRICE = :Price, 
                STOCK = :Stock, 
                UPDATED_AT = SYSTIMESTAMP 
            WHERE ID = :Id";
        
        await connection.ExecuteAsync(sql, product);
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM PRODUCTS WHERE ID = :Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
