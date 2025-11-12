using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MauseTalkBackend.Domain.Interfaces;
using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Entities;
using MauseTalkBackend.Shared.Models;
using MauseTalkBackend.Shared.Constants;
using MauseTalkBackend.Shared.Extensions;

namespace MauseTalkBackend.App.Controllers;

[ApiController]
[Route(ApiConstants.ApiPrefix + ApiConstants.Routes.Chats)]
[Authorize(Policy = ApiConstants.Policies.RequireAuthenticated)]
public class ChatsController : ControllerBase
{
    private readonly IChatRepository _chatRepository;

    public ChatsController(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ChatDto>>>> GetUserChats()
    {
        try
        {
            var userId = User.GetUserId();
            var chats = await _chatRepository.GetUserChatsAsync(userId);
            var chatDtos = chats.Select(MapToChatDto);
            
            return Ok(ApiResponse<IEnumerable<ChatDto>>.SuccessResult(chatDtos));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<ChatDto>>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpGet("{chatId:guid}")]
    public async Task<ActionResult<ApiResponse<ChatDto>>> GetChat(Guid chatId)
    {
        try
        {
            var userId = User.GetUserId();
            var chat = await _chatRepository.GetByIdAsync(chatId);
            
            if (chat == null)
            {
                return NotFound(ApiResponse<ChatDto>.ErrorResult("Chat not found"));
            }

            // Check if user has access to the chat
            if (!chat.ChatUsers.Any(cu => cu.UserId == userId))
            {
                return Forbid();
            }

            return Ok(ApiResponse<ChatDto>.SuccessResult(MapToChatDto(chat)));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ChatDto>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ChatDto>>> CreateChat([FromBody] CreateChatDto createChatDto)
    {
        try
        {
            var userId = User.GetUserId();
            var chat = await _chatRepository.CreateAsync(createChatDto, userId);
            var chatDto = MapToChatDto(chat);
            
            return Ok(ApiResponse<ChatDto>.SuccessResult(chatDto, "Chat created successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ChatDto>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpPut("{chatId:guid}")]
    public async Task<ActionResult<ApiResponse<ChatDto>>> UpdateChat(Guid chatId, [FromBody] UpdateChatDto updateChatDto)
    {
        try
        {
            var userId = User.GetUserId();
            
            // Check if user is admin or owner
            if (!await _chatRepository.HasUserRole(chatId, userId, ChatUserRole.Admin))
            {
                return Forbid();
            }
            
            var updatedChat = await _chatRepository.UpdateChatSettingsAsync(chatId, updateChatDto);
            return Ok(ApiResponse<ChatDto>.SuccessResult(MapToChatDto(updatedChat), "Chat updated successfully"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<ChatDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ChatDto>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpDelete("{chatId:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteChat(Guid chatId)
    {
        try
        {
            var userId = User.GetUserId();
            var chat = await _chatRepository.GetByIdAsync(chatId);
            
            if (chat == null)
            {
                return NotFound(ApiResponse.ErrorResult("Chat not found"));
            }

            // Check if user is owner
            var chatUser = chat.ChatUsers.FirstOrDefault(cu => cu.UserId == userId);
            if (chatUser == null || chatUser.Role != ChatUserRole.Owner)
            {
                return Forbid();
            }

            await _chatRepository.DeleteAsync(chatId);
            return Ok(ApiResponse.SuccessResult("Chat deleted successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpPost("{chatId:guid}/users/{userIdToAdd:guid}")]
    public async Task<ActionResult<ApiResponse<ChatUserDto>>> AddUserToChat(Guid chatId, Guid userIdToAdd)
    {
        try
        {
            var userId = User.GetUserId();
            var chat = await _chatRepository.GetByIdAsync(chatId);
            
            if (chat == null)
            {
                return NotFound(ApiResponse<ChatUserDto>.ErrorResult("Chat not found"));
            }

            // Check if current user is admin or owner
            var chatUser = chat.ChatUsers.FirstOrDefault(cu => cu.UserId == userId);
            if (chatUser == null || (chatUser.Role != ChatUserRole.Admin && chatUser.Role != ChatUserRole.Owner))
            {
                return Forbid();
            }

            var addedChatUser = await _chatRepository.AddUserToChatAsync(chatId, userIdToAdd);
            return Ok(ApiResponse<ChatUserDto>.SuccessResult(MapToChatUserDto(addedChatUser), "User added to chat successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ChatUserDto>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpDelete("{chatId:guid}/users/{userIdToRemove:guid}")]
    public async Task<ActionResult<ApiResponse>> RemoveUserFromChat(Guid chatId, Guid userIdToRemove)
    {
        try
        {
            var userId = User.GetUserId();
            var chat = await _chatRepository.GetByIdAsync(chatId);
            
            if (chat == null)
            {
                return NotFound(ApiResponse.ErrorResult("Chat not found"));
            }

            // Check permissions: owner can remove anyone, admin can remove members, user can remove themselves
            var currentChatUser = chat.ChatUsers.FirstOrDefault(cu => cu.UserId == userId);
            var targetChatUser = chat.ChatUsers.FirstOrDefault(cu => cu.UserId == userIdToRemove);
            
            if (currentChatUser == null || targetChatUser == null)
            {
                return NotFound(ApiResponse.ErrorResult("User not found in chat"));
            }

            bool canRemove = userId == userIdToRemove || // User removing themselves
                           currentChatUser.Role == ChatUserRole.Owner || // Owner can remove anyone
                           (currentChatUser.Role == ChatUserRole.Admin && targetChatUser.Role == ChatUserRole.Member); // Admin can remove members

            if (!canRemove)
            {
                return Forbid();
            }

            await _chatRepository.RemoveUserFromChatAsync(chatId, userIdToRemove);
            return Ok(ApiResponse.SuccessResult("User removed from chat successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpPost("{chatId:guid}/read")]
    public async Task<ActionResult<ApiResponse>> MarkAsRead(Guid chatId)
    {
        try
        {
            var userId = User.GetUserId();
            await _chatRepository.UpdateLastReadAsync(chatId, userId);
            return Ok(ApiResponse.SuccessResult("Chat marked as read"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpPut("{chatId:guid}/users/role")]
    public async Task<ActionResult<ApiResponse<ChatUserDto>>> UpdateUserRole(Guid chatId, [FromBody] UpdateChatUserRoleDto updateRoleDto)
    {
        try
        {
            var userId = User.GetUserId();
            
            // Only owners can change roles
            if (!await _chatRepository.HasUserRole(chatId, userId, ChatUserRole.Owner))
            {
                return Forbid();
            }
            
            // Cannot change owner role
            if (updateRoleDto.Role == ChatUserRole.Owner)
            {
                return BadRequest(ApiResponse<ChatUserDto>.ErrorResult("Cannot assign owner role"));
            }
            
            var updatedChatUser = await _chatRepository.UpdateUserRoleAsync(chatId, updateRoleDto.UserId, updateRoleDto.Role);
            return Ok(ApiResponse<ChatUserDto>.SuccessResult(MapToChatUserDto(updatedChatUser), "User role updated successfully"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<ChatUserDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ChatUserDto>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    private static ChatDto MapToChatDto(Chat chat)
    {
        return new ChatDto
        {
            Id = chat.Id,
            Name = chat.Name,
            Description = chat.Description,
            AvatarUrl = chat.AvatarUrl,
            Type = chat.Type,
            CreatedBy = MapToUserDto(chat.CreatedBy),
            CreatedAt = DateTime.SpecifyKind(chat.CreatedAt, DateTimeKind.Utc),
            LastActivityAt = DateTime.SpecifyKind(chat.LastActivityAt, DateTimeKind.Utc),
            AllowInvites = chat.AllowInvites,
            AllowMembersToInvite = chat.AllowMembersToInvite,
            MaxMembers = chat.MaxMembers,
            Users = chat.ChatUsers?.Select(MapToChatUserDto) ?? Enumerable.Empty<ChatUserDto>(),
            LastMessage = chat.Messages?.OrderByDescending(m => m.CreatedAt).FirstOrDefault() is Message lastMsg 
                ? MapToMessageDto(lastMsg) 
                : null
        };
    }

    private static ChatUserDto MapToChatUserDto(ChatUser chatUser)
    {
        return new ChatUserDto
        {
            Id = chatUser.Id,
            User = MapToUserDto(chatUser.User),
            Role = chatUser.Role,
            JoinedAt = chatUser.JoinedAt,
            LastReadAt = chatUser.LastReadAt
        };
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
            Reactions = message.Reactions?.Select(r => new ReactionDto
            {
                Id = r.Id,
                MessageId = r.MessageId,
                UserId = r.UserId.ToString(),
                User = MapToUserDto(r.User),
                Type = r.Type,
                CreatedAt = DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc)
            }) ?? Enumerable.Empty<ReactionDto>()
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
            CreatedAt = DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc),
            LastSeenAt = DateTime.SpecifyKind(user.LastSeenAt, DateTimeKind.Utc),
            IsOnline = user.IsOnline
        };
    }
}