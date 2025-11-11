using System.Net.Http.Json;
using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Entities;
using MauseTalkBackend.Shared.Models;

namespace MauseTalkBackend.Client.Services;

public class MessageService
{
    private readonly HttpClient _httpClient;

    public MessageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<MessageDto>> GetMessagesAsync(Guid chatId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/messages/{chatId}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<MessageDto>>>();
                return result?.Data?.ToList() ?? new List<MessageDto>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get messages error: {ex.Message}");
        }
        return new List<MessageDto>();
    }

    public async Task<MessageDto?> SendMessageAsync(CreateMessageDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/messages", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<MessageDto>>();
                return result?.Data;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Send message error: {ex.Message}");
        }
        return null;
    }

    public async Task<bool> ToggleReactionAsync(Guid messageId, ReactionType reactionType)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/messages/{messageId}/reactions", new CreateReactionDto
            {
                MessageId = messageId,
                Type = reactionType
            });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Toggle reaction error: {ex.Message}");
            return false;
        }
    }
}