using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Entities;
using MauseTalkBackend.Domain.Interfaces;
using MauseTalkBackend.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MauseTalkBackend.Api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly MauseTalkDbContext _context;

    public UserRepository(MauseTalkDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<IEnumerable<User>> SearchAsync(string query)
    {
        return await _context.Users
            .Where(u => u.Username.Contains(query) || 
                       (u.DisplayName != null && u.DisplayName.Contains(query)) ||
                       u.Email.Contains(query))
            .ToListAsync();
    }

    public async Task<User> CreateAsync(CreateUserDto createUserDto, string passwordHash)
    {
        var user = new User
        {
            Username = createUserDto.Username,
            Email = createUserDto.Email,
            DisplayName = createUserDto.DisplayName,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            IsOnline = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateLastSeenAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastSeenAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateOnlineStatusAsync(Guid userId, bool isOnline)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsOnline = isOnline;
            if (isOnline)
            {
                user.LastSeenAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }
    }
}