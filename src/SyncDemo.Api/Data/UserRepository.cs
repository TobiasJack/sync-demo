using Dapper;
using SyncDemo.Shared.Models;
using Oracle.ManagedDataAccess.Client;

namespace SyncDemo.Api.Data;

/// <summary>
/// Repository for User operations
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT USER_ID as UserId, USERNAME as Username, EMAIL as Email, ROLE as Role, CREATED_AT as CreatedAt FROM USERS WHERE USERNAME = :Username";
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT USER_ID as UserId, USERNAME as Username, EMAIL as Email, ROLE as Role, CREATED_AT as CreatedAt FROM USERS WHERE USER_ID = :UserId";
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId });
    }

    public async Task<User> CreateUserAsync(User user)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"INSERT INTO USERS (USERNAME, EMAIL, ROLE)
                           VALUES (:Username, :Email, :Role)
                           RETURNING USER_ID INTO :UserId";
        
        var parameters = new DynamicParameters();
        parameters.Add("Username", user.Username);
        parameters.Add("Email", user.Email);
        parameters.Add("Role", user.Role);
        parameters.Add("UserId", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);
        
        await connection.ExecuteAsync(sql, parameters);
        user.UserId = parameters.Get<int>("UserId");
        user.CreatedAt = DateTime.UtcNow;
        
        return user;
    }
}
