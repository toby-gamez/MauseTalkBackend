using Microsoft.AspNetCore.SignalR.Client;
using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Entities;
using MauseTalkBackend.Client.Services;

namespace MauseTalkBackend.Client.Services;

public class SignalRService : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly AuthService _authService;

    public SignalRService(AuthService authService, string hubUrl)
    {
        _authService = authService;
        
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(_authService.GetAuthToken());
            })
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers();
    }

    // Events
    public event Action<MessageDto>? MessageReceived;
    public event Action<string, string>? UserJoined;
    public event Action<string, string>? UserLeft;
    public event Action<ReactionDto>? ReactionAdded;
    public event Action<Guid, Guid, ReactionType>? ReactionRemoved;
    public event Action<bool>? ConnectionStateChanged;

    public bool IsConnected => _hubConnection.State == HubConnectionState.Connected;
    public string ConnectionState => _hubConnection.State.ToString();

    public async Task StartAsync()
    {
        try
        {
            await _hubConnection.StartAsync();
            ConnectionStateChanged?.Invoke(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR connection error: {ex.Message}");
            ConnectionStateChanged?.Invoke(false);
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            ConnectionStateChanged?.Invoke(false);
        }
    }

    public async Task JoinChatAsync(string chatId)
    {
        if (IsConnected)
        {
            await _hubConnection.InvokeAsync("JoinChat", chatId);
        }
    }

    public async Task LeaveChatAsync(string chatId)
    {
        if (IsConnected)
        {
            await _hubConnection.InvokeAsync("LeaveChat", chatId);
        }
    }

    public async Task JoinChatAsync(Guid chatId)
    {
        if (IsConnected)
        {
            await _hubConnection.InvokeAsync("JoinChat", chatId);
        }
    }

    public async Task LeaveChatAsync(Guid chatId)
    {
        if (IsConnected)
        {
            await _hubConnection.InvokeAsync("LeaveChat", chatId);
        }
    }

    private void RegisterHandlers()
    {
        // Receive new message
        _hubConnection.On<MessageDto>("ReceiveMessage", (message) =>
        {
            Console.WriteLine($"SignalR: Received message - {message.Content} from {message.User?.Username}");
            MessageReceived?.Invoke(message);
        });

        // User joined chat
        _hubConnection.On<string, string>("UserJoined", (username, chatId) =>
        {
            UserJoined?.Invoke(username, chatId);
        });

        // User left chat  
        _hubConnection.On<string, string>("UserLeft", (username, chatId) =>
        {
            UserLeft?.Invoke(username, chatId);
        });

        // Reaction added
        _hubConnection.On<ReactionDto>("ReactionAdded", (reaction) =>
        {
            Console.WriteLine($"SignalR: Reaction added - {reaction.Type} by {reaction.User?.Username}");
            ReactionAdded?.Invoke(reaction);
        });

        // Reaction removed
        _hubConnection.On<Guid, Guid, ReactionType>("ReactionRemoved", (messageId, userId, reactionType) =>
        {
            Console.WriteLine($"SignalR: Reaction removed - {reactionType} from message {messageId}");
            ReactionRemoved?.Invoke(messageId, userId, reactionType);
        });

        // Connection events
        _hubConnection.Reconnecting += (error) =>
        {
            ConnectionStateChanged?.Invoke(false);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += (connectionId) =>
        {
            ConnectionStateChanged?.Invoke(true);
            return Task.CompletedTask;
        };

        _hubConnection.Closed += (error) =>
        {
            ConnectionStateChanged?.Invoke(false);
            return Task.CompletedTask;
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}