using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MauseTalkBackend.Domain.Interfaces;
using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Shared.Models;
using MauseTalkBackend.Shared.Constants;
using MauseTalkBackend.Shared.Extensions;

namespace MauseTalkBackend.App.Controllers;

[ApiController]
[Route(ApiConstants.ApiPrefix + ApiConstants.Routes.Users)]
[Authorize(Policy = ApiConstants.Policies.RequireAuthenticated)]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetUsers()
    {
        try
        {
            var users = await _userRepository.GetAllAsync();
            var userDtos = users.Select(MapToUserDto);
            return Ok(ApiResponse<IEnumerable<UserDto>>.SuccessResult(userDtos));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<UserDto>>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid id)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.ErrorResult("User not found"));
            }

            return Ok(ApiResponse<UserDto>.SuccessResult(MapToUserDto(user)));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<UserDto>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> SearchUsers([FromQuery] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(ApiResponse<IEnumerable<UserDto>>.ErrorResult("Search query is required"));
            }

            var users = await _userRepository.SearchAsync(query);
            var userDtos = users.Select(MapToUserDto);
            return Ok(ApiResponse<IEnumerable<UserDto>>.SuccessResult(userDtos));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<UserDto>>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
    {
        try
        {
            var userId = User.GetUserId();
            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.ErrorResult("User not found"));
            }

            return Ok(ApiResponse<UserDto>.SuccessResult(MapToUserDto(user)));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<UserDto>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateCurrentUser([FromBody] UserDto updateUserDto)
    {
        try
        {
            var userId = User.GetUserId();
            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.ErrorResult("User not found"));
            }

            // Update allowed fields
            user.DisplayName = updateUserDto.DisplayName;
            user.AvatarUrl = updateUserDto.AvatarUrl;

            var updatedUser = await _userRepository.UpdateAsync(user);
            return Ok(ApiResponse<UserDto>.SuccessResult(MapToUserDto(updatedUser)));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<UserDto>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    private static UserDto MapToUserDto(Domain.Entities.User user)
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