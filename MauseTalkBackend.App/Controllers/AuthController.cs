using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MauseTalkBackend.Api.Services;
using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Shared.Models;
using MauseTalkBackend.Shared.Constants;

namespace MauseTalkBackend.App.Controllers;

[ApiController]
[Route(ApiConstants.ApiPrefix + ApiConstants.Routes.Auth)]
[AllowAnonymous] // Allow registration and login without authentication
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Register([FromBody] RegisterRequest registerRequest)
    {
        try
        {
            // Convert RegisterRequest to CreateUserDto
            var createUserDto = new CreateUserDto
            {
                Username = registerRequest.Username,
                Email = registerRequest.Email,
                Password = registerRequest.Password,
                DisplayName = registerRequest.DisplayName
            };
            
            var user = await _authService.RegisterAsync(createUserDto);
            return Ok(ApiResponse<UserDto>.SuccessResult(user, "User registered successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<UserDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<UserDto>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest loginRequest)
    {
        try
        {
            // Convert LoginRequest to LoginDto
            var loginDto = new LoginDto
            {
                Username = loginRequest.Username,
                Password = loginRequest.Password
            };
            
            var result = await _authService.LoginAsync(loginDto);
            if (result == null)
            {
                return Unauthorized(ApiResponse<AuthResponse>.ErrorResult("Invalid username or password"));
            }

            // Convert LoginResultDto to AuthResponse
            var authResponse = new AuthResponse
            {
                User = result.User,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken
            };

            return Ok(ApiResponse<AuthResponse>.SuccessResult(authResponse, "Login successful"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<AuthResponse>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse>> Logout()
    {
        try
        {
            // TODO: Get user ID from JWT token
            // await _authService.LogoutAsync(userId);
            return Ok(ApiResponse.SuccessResult("Logout successful"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { message = "API is working", timestamp = DateTime.Now });
    }
}