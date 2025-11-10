using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Entities;
using MauseTalkBackend.Domain.Interfaces;
using BCrypt.Net;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace MauseTalkBackend.Api.Services;

public class AuthService
{
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<LoginResultDto?> LoginAsync(LoginDto loginDto)
    {
        var user = await _userRepository.GetByUsernameAsync(loginDto.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            return null;
        }

        await _userRepository.UpdateOnlineStatusAsync(user.Id, true);

        return new LoginResultDto
        {
            User = MapToUserDto(user),
            AccessToken = GenerateAccessToken(user),
            RefreshToken = GenerateRefreshToken()
        };
    }

    public async Task<UserDto> RegisterAsync(CreateUserDto createUserDto)
    {
        // Check if user already exists
        var existingUser = await _userRepository.GetByUsernameAsync(createUserDto.Username);
        if (existingUser != null)
        {
            throw new ArgumentException("Username already exists");
        }

        existingUser = await _userRepository.GetByEmailAsync(createUserDto.Email);
        if (existingUser != null)
        {
            throw new ArgumentException("Email already exists");
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);

        // Create user
        var user = await _userRepository.CreateAsync(createUserDto, passwordHash);
        return MapToUserDto(user);
    }

    public async Task LogoutAsync(Guid userId)
    {
        await _userRepository.UpdateOnlineStatusAsync(userId, false);
    }

    private string GenerateAccessToken(User user)
    {
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("YourSuperSecretKeyThatIsAtLeast32Characters!");
        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("user_id", user.Id.ToString()),
                new System.Security.Claims.Claim("username", user.Username),
                new System.Security.Claims.Claim("email", user.Email)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "MauseTalkBackend",
            Audience = "MauseTalkBackend",
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString();
    }

    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            LastSeenAt = user.LastSeenAt,
            IsOnline = user.IsOnline
        };
    }
}