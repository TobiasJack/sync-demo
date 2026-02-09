using SyncDemo.Shared.Models;

namespace SyncDemo.Api.Data;

/// <summary>
/// Repository interface for User operations
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int userId);
    Task<User> CreateUserAsync(User user);
}
