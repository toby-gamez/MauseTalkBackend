using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Shared.Models;
using Microsoft.JSInterop;

namespace MauseTalkBackend.Client.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private string? _currentToken;
    private UserDto? _currentUser;

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public event Action<bool>? AuthStateChanged;
    public UserDto? CurrentUser => _currentUser;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_currentToken);

    public async Task<bool> InitializeAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "mausetalk-token");
            if (!string.IsNullOrEmpty(token))
            {
                _currentToken = token;
                SetAuthHeader(token);
                
                // Verify token by getting current user
                var user = await GetCurrentUserAsync();
                if (user != null)
                {
                    _currentUser = user;
                    AuthStateChanged?.Invoke(true);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auth initialization error: {ex.Message}");
        }

        await LogoutAsync();
        return false;
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            Console.WriteLine($"Sending login request to: {_httpClient.BaseAddress}api/auth/login");
            Console.WriteLine($"Request data: {System.Text.Json.JsonSerializer.Serialize(request)}");
            
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
            
            Console.WriteLine($"Response status: {response.StatusCode}");
            Console.WriteLine($"Response headers: {response.Headers}");
            Console.WriteLine($"Content type: {response.Content.Headers.ContentType}");
            
            // Read raw response content for debugging
            var rawContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Raw response content: {rawContent}");
            
            // Try parsing JSON
            var result = JsonSerializer.Deserialize<ApiResponse<LoginResultDto>>(rawContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (result?.Success == true && result.Data != null)
            {
                _currentToken = result.Data.AccessToken;
                _currentUser = result.Data.User;
                
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "mausetalk-token", _currentToken);
                SetAuthHeader(_currentToken);
                AuthStateChanged?.Invoke(true);
                
                // Convert LoginResultDto to AuthResponse for return type compatibility
                var authResponse = new AuthResponse
                {
                    User = result.Data.User,
                    AccessToken = result.Data.AccessToken,
                    RefreshToken = result.Data.RefreshToken
                };
                
                return new ApiResponse<AuthResponse> 
                { 
                    Success = true, 
                    Message = result.Message,
                    Data = authResponse 
                };
            }
            
            return new ApiResponse<AuthResponse> 
            { 
                Success = false, 
                Message = result?.Message ?? "Neočekávaná chyba" 
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            return new ApiResponse<AuthResponse> 
            { 
                Success = false, 
                Message = $"Chyba při přihlašování: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // First register user
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);
            var registerResult = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
            
            if (registerResult?.Success == true)
            {
                // Then automatically login
                var loginRequest = new LoginRequest 
                { 
                    Username = request.Username, 
                    Password = request.Password 
                };
                return await LoginAsync(loginRequest);
            }
            
            return new ApiResponse<AuthResponse> 
            { 
                Success = false, 
                Message = registerResult?.Message ?? "Registrace selhala" 
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<AuthResponse> 
            { 
                Success = false, 
                Message = "Chyba při registraci",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public void Logout() => _ = LogoutAsync();

    public async Task LogoutAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_currentToken))
            {
                await _httpClient.PostAsync("/api/auth/logout", null);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logout error: {ex.Message}");
        }
        finally
        {
            _currentToken = null;
            _currentUser = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
            
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "mausetalk-token");
            AuthStateChanged?.Invoke(false);
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/users/me");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
                return result?.Data;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get current user error: {ex.Message}");
        }
        return null;
    }

    public async Task<ApiResponse<UserDto>> UpdateUserAsync(UpdateUserRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("/api/users/me", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
            
            if (result?.Success == true && result.Data != null)
            {
                _currentUser = result.Data;
            }
            
            return result ?? new ApiResponse<UserDto> { Success = false, Message = "Neočekávaná chyba" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Chyba při aktualizaci profilu",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public string GetAuthToken() => _currentToken ?? string.Empty;

    private void SetAuthHeader(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}