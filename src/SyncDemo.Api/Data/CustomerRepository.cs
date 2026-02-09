using Dapper;
using SyncDemo.Shared.Models;

namespace SyncDemo.Api.Data;

public interface ICustomerRepository
{
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<int> CreateAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(int id);
}

public class CustomerRepository : ICustomerRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CustomerRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT ID as Id, NAME as Name, EMAIL as Email, PHONE as Phone, 
                   CREATED_AT as CreatedAt, UPDATED_AT as UpdatedAt 
            FROM CUSTOMERS 
            ORDER BY CREATED_AT DESC";
        
        return await connection.QueryAsync<Customer>(sql);
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT ID as Id, NAME as Name, EMAIL as Email, PHONE as Phone, 
                   CREATED_AT as CreatedAt, UPDATED_AT as UpdatedAt 
            FROM CUSTOMERS 
            WHERE ID = :Id";
        
        return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { Id = id });
    }

    public async Task<int> CreateAsync(Customer customer)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO CUSTOMERS (NAME, EMAIL, PHONE)
            VALUES (:Name, :Email, :Phone)
            RETURNING ID INTO :Id";
        
        var parameters = new DynamicParameters();
        parameters.Add("Name", customer.Name);
        parameters.Add("Email", customer.Email);
        parameters.Add("Phone", customer.Phone);
        parameters.Add("Id", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);
        
        await connection.ExecuteAsync(sql, parameters);
        
        return parameters.Get<int>("Id");
    }

    public async Task UpdateAsync(Customer customer)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE CUSTOMERS 
            SET NAME = :Name, 
                EMAIL = :Email, 
                PHONE = :Phone, 
                UPDATED_AT = SYSTIMESTAMP 
            WHERE ID = :Id";
        
        await connection.ExecuteAsync(sql, customer);
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM CUSTOMERS WHERE ID = :Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
