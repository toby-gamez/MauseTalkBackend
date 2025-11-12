using System.Net.Http.Json;
using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Shared.Models;

namespace MauseTalkBackend.Client.Services;

public class ChatService
{
    private readonly HttpClient _httpClient;

    public ChatService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ChatDto>> GetChatsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/chats");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<ChatDto>>>();
                return result?.Data?.ToList() ?? new List<ChatDto>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get chats error: {ex.Message}");
        }
        return new List<ChatDto>();
    }

    public async Task<ChatDto?> GetChatAsync(Guid chatId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/chats/{chatId}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<ChatDto>>();
                return result?.Data;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get chat error: {ex.Message}");
        }
        return null;
    }

    public async Task<ApiResponse<ChatDto>> CreateChatAsync(CreateChatDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/chats", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ChatDto>>();
            return result ?? new ApiResponse<ChatDto> { Success = false, Message = "Neočekávaná chyba" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ChatDto>
            {
                Success = false,
                Message = "Chyba při vytváření chatu",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse> AddUserToChatAsync(Guid chatId, Guid userId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/chats/{chatId}/users/{userId}", null);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            return result ?? new ApiResponse { Success = false, Message = "Neočekávaná chyba" };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                Success = false,
                Message = "Chyba při přidávání uživatele",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<ChatDto>> UpdateChatAsync(Guid chatId, UpdateChatDto request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/chats/{chatId}", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ChatDto>>();
            return result ?? new ApiResponse<ChatDto> { Success = false, Message = "Neočekávaná chyba" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ChatDto>
            {
                Success = false,
                Message = "Chyba při aktualizaci chatu",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<ChatUserDto>> UpdateUserRoleAsync(Guid chatId, UpdateChatUserRoleDto request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/chats/{chatId}/users/role", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ChatUserDto>>();
            return result ?? new ApiResponse<ChatUserDto> { Success = false, Message = "Neočekávaná chyba" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ChatUserDto>
            {
                Success = false,
                Message = "Chyba při změně role uživatele",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse> RemoveUserFromChatAsync(Guid chatId, Guid userId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/chats/{chatId}/users/{userId}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            return result ?? new ApiResponse { Success = false, Message = "Neočekávaná chyba" };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                Success = false,
                Message = "Chyba při odebírání uživatele",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}