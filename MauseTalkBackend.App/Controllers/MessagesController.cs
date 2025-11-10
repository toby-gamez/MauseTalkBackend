using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MauseTalkBackend.Domain.Interfaces;
using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Entities;
using MauseTalkBackend.Shared.Models;
using MauseTalkBackend.Shared.Constants;
using MauseTalkBackend.Shared.Extensions;
using MauseTalkBackend.App.Hubs;

namespace MauseTalkBackend.App.Controllers;

[ApiController]
[Route(ApiConstants.ApiPrefix + ApiConstants.Routes.Messages)]
[Authorize(Policy = ApiConstants.Policies.RequireAuthenticated)]
public class MessagesController : ControllerBase
{
    private readonly IMessageRepository _messageRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessagesController(
        IMessageRepository messageRepository,
        IChatRepository chatRepository,
        IHubContext<ChatHub> hubContext)
    {
        _messageRepository = messageRepository;
        _chatRepository = chatRepository;
        _hubContext = hubContext;
    }

    [HttpGet("{chatId:guid}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<MessageDto>>>> GetChatMessages(
        Guid chatId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        try
        {
            var userId = User.GetUserId();
            
            // Check if user has access to the chat
            var chatUsers = await _chatRepository.GetChatUsersAsync(chatId);
            if (!chatUsers.Any(cu => cu.UserId == userId))
            {
                return Forbid();
            }

            var messages = await _messageRepository.GetChatMessagesAsync(chatId, skip, take);
            var messageDtos = messages.Select(MapToMessageDto);
            
            return Ok(ApiResponse<IEnumerable<MessageDto>>.SuccessResult(messageDtos));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<MessageDto>>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MessageDto>>> SendMessage([FromBody] CreateMessageDto createMessageDto)
    {
        try
        {
            var userId = User.GetUserId();
            
            // Check if user has access to the chat
            var chatUsers = await _chatRepository.GetChatUsersAsync(createMessageDto.ChatId);
            if (!chatUsers.Any(cu => cu.UserId == userId))
            {
                return Forbid();
            }

            var message = await _messageRepository.CreateAsync(createMessageDto, userId);
            var messageDto = MapToMessageDto(message);

            // Send real-time notification
            await _hubContext.Clients.Group($"Chat_{createMessageDto.ChatId}")
                .SendAsync("NewMessage", messageDto);

            return Ok(ApiResponse<MessageDto>.SuccessResult(messageDto, "Message sent successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MessageDto>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpPut("{messageId:guid}")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> UpdateMessage(Guid messageId, [FromBody] CreateMessageDto updateMessageDto)
    {
        try
        {
            var userId = User.GetUserId();
            var message = await _messageRepository.GetByIdAsync(messageId);
            
            if (message == null)
            {
                return NotFound(ApiResponse<MessageDto>.ErrorResult("Message not found"));
            }

            if (message.UserId != userId)
            {
                return Forbid();
            }

            message.Content = updateMessageDto.Content;
            var updatedMessage = await _messageRepository.UpdateAsync(message);
            var messageDto = MapToMessageDto(updatedMessage);

            // Send real-time update
            await _hubContext.Clients.Group($"Chat_{message.ChatId}")
                .SendAsync("MessageUpdated", messageDto);

            return Ok(ApiResponse<MessageDto>.SuccessResult(messageDto, "Message updated successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MessageDto>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpDelete("{messageId:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteMessage(Guid messageId)
    {
        try
        {
            var userId = User.GetUserId();
            var message = await _messageRepository.GetByIdAsync(messageId);
            
            if (message == null)
            {
                return NotFound(ApiResponse.ErrorResult("Message not found"));
            }

            if (message.UserId != userId)
            {
                return Forbid();
            }

            await _messageRepository.DeleteAsync(messageId);

            // Send real-time notification
            await _hubContext.Clients.Group($"Chat_{message.ChatId}")
                .SendAsync("MessageDeleted", new { MessageId = messageId });

            return Ok(ApiResponse.SuccessResult("Message deleted successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpPost("{messageId:guid}/reactions")]
    public async Task<ActionResult<ApiResponse<ReactionDto>>> AddReaction(Guid messageId, [FromBody] CreateReactionDto createReactionDto)
    {
        try
        {
            var userId = User.GetUserId();
            var reaction = await _messageRepository.AddReactionAsync(messageId, userId, createReactionDto.Type);
            var reactionDto = MapToReactionDto(reaction);

            // Send real-time notification
            var message = await _messageRepository.GetByIdAsync(messageId);
            if (message != null)
            {
                await _hubContext.Clients.Group($"Chat_{message.ChatId}")
                    .SendAsync("ReactionAdded", reactionDto);
            }

            return Ok(ApiResponse<ReactionDto>.SuccessResult(reactionDto, "Reaction added successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ReactionDto>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpDelete("{messageId:guid}/reactions/{reactionType}")]
    public async Task<ActionResult<ApiResponse>> RemoveReaction(Guid messageId, ReactionType reactionType)
    {
        try
        {
            var userId = User.GetUserId();
            await _messageRepository.RemoveReactionAsync(messageId, userId, reactionType);

            // Send real-time notification
            var message = await _messageRepository.GetByIdAsync(messageId);
            if (message != null)
            {
                await _hubContext.Clients.Group($"Chat_{message.ChatId}")
                    .SendAsync("ReactionRemoved", new { MessageId = messageId, UserId = userId, Type = reactionType });
            }

            return Ok(ApiResponse.SuccessResult("Reaction removed successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    private static MessageDto MapToMessageDto(Message message)
    {
        return new MessageDto
        {
            Id = message.Id,
            ChatId = message.ChatId,
            User = MapToUserDto(message.User),
            Content = message.Content,
            Type = message.Type,
            FileUrl = message.FileUrl,
            FileName = message.FileName,
            FileSize = message.FileSize,
            MimeType = message.MimeType,
            CreatedAt = message.CreatedAt,
            EditedAt = message.EditedAt,
            Reactions = message.Reactions?.Select(MapToReactionDto) ?? Enumerable.Empty<ReactionDto>()
        };
    }

    private static ReactionDto MapToReactionDto(Reaction reaction)
    {
        return new ReactionDto
        {
            Id = reaction.Id,
            MessageId = reaction.MessageId,
            User = MapToUserDto(reaction.User),
            Type = reaction.Type,
            CreatedAt = reaction.CreatedAt
        };
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