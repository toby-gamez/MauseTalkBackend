using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Entities;

namespace MauseTalkBackend.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task<IEnumerable<User>> SearchAsync(string query);
    Task<User> CreateAsync(CreateUserDto createUserDto, string passwordHash);
    Task<User> UpdateAsync(User user);
    Task DeleteAsync(Guid id);
    Task UpdateLastSeenAsync(Guid userId);
    Task UpdateOnlineStatusAsync(Guid userId, bool isOnline);
}