using MauseTalkBackend.Domain.DTOs;
using System.Net.Http.Json;

namespace MauseTalkBackend.Client.Services;

public interface IInviteService
{
    Task<InviteLinkDto> CreateInviteLinkAsync(CreateInviteLinkDto createInviteLink);
    Task<InviteLinkInfoDto> GetInviteLinkInfoAsync(string code);
    Task<ChatDto> JoinChatViaInviteAsync(string code);
    Task<List<InviteLinkDto>> GetChatInviteLinksAsync(Guid chatId);
    Task DeleteInviteLinkAsync(Guid id);
    Task DeactivateInviteLinkAsync(Guid id);
}

public class InviteService : IInviteService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    public InviteService(HttpClient httpClient, AuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    private void SetAuthorizationHeader()
    {
        if (_authService.IsAuthenticated && _authService.CurrentUser != null)
        {
            var token = _authService.GetAuthToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
    }

    public async Task<InviteLinkDto> CreateInviteLinkAsync(CreateInviteLinkDto createInviteLink)
    {
        try
        {
            Console.WriteLine("InviteService: Setting auth header...");
            SetAuthorizationHeader();
            
            Console.WriteLine($"InviteService: Calling POST /api/invites with ChatId={createInviteLink.ChatId}");
            var response = await _httpClient.PostAsJsonAsync("/api/invites", createInviteLink);
            
            Console.WriteLine($"InviteService: Response status: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"InviteService: Error response: {errorContent}");
                throw new HttpRequestException($"API call failed with status {response.StatusCode}: {errorContent}");
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"InviteService: Success response: {responseContent}");
            
            var result = await response.Content.ReadFromJsonAsync<InviteLinkDto>() ?? throw new InvalidOperationException("Failed to create invite link");
            Console.WriteLine($"InviteService: Parsed result - InviteCode: {result.InviteCode}");
            
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"InviteService.CreateInviteLinkAsync error: {ex.Message}");
            throw;
        }
    }

    public async Task<InviteLinkInfoDto> GetInviteLinkInfoAsync(string code)
    {
        var response = await _httpClient.GetAsync($"/api/invites/{code}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InviteLinkInfoDto>() ?? throw new InvalidOperationException("Failed to get invite link info");
    }

    public async Task<ChatDto> JoinChatViaInviteAsync(string code)
    {
        var response = await _httpClient.PostAsync($"/api/invites/{code}/join", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChatDto>() ?? throw new InvalidOperationException("Failed to join chat");
    }

    public async Task<List<InviteLinkDto>> GetChatInviteLinksAsync(Guid chatId)
    {
        var response = await _httpClient.GetAsync($"/api/invites/chat/{chatId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InviteLinkDto>>() ?? new List<InviteLinkDto>();
    }

    public async Task DeleteInviteLinkAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"/api/invites/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task DeactivateInviteLinkAsync(Guid id)
    {
        var response = await _httpClient.PutAsync($"/api/invites/{id}/deactivate", null);
        response.EnsureSuccessStatusCode();
    }
}