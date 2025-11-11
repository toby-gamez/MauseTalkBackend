using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MauseTalkBackend.Shared.Extensions;

namespace MauseTalkBackend.App.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public async Task JoinChat(string chatId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Chat_{chatId}");
    }

    public async Task LeaveChat(string chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Chat_{chatId}");
    }

    public async Task SendTyping(string chatId, string username)
    {
        await Clients.Group($"Chat_{chatId}")
            .SendAsync("UserTyping", new { Username = username, IsTyping = true });
    }

    public async Task StopTyping(string chatId, string username)
    {
        await Clients.Group($"Chat_{chatId}")
            .SendAsync("UserTyping", new { Username = username, IsTyping = false });
    }

    public async Task SendMessage(object message)
    {
        // For now, just acknowledge the message was received
        // In a full implementation, this would save to database and broadcast
        Console.WriteLine($"Received message from SignalR: {System.Text.Json.JsonSerializer.Serialize(message)}");
        
        // TODO: Implement proper message handling
        // 1. Validate message
        // 2. Save to database via MessageService
        // 3. Broadcast to chat group
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.GetUserId();
        if (userId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.GetUserId();
        if (userId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}