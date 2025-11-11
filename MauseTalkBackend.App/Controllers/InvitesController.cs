using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Interfaces;
using MauseTalkBackend.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MauseTalkBackend.App.Controllers;

[ApiController]
[Route(ApiConstants.ApiPrefix + "/invites")]
[Authorize(Policy = ApiConstants.Policies.RequireAuthenticated)]
public class InvitesController : ControllerBase
{
    private readonly IInviteLinkRepository _inviteLinkRepository;
    private readonly IChatRepository _chatRepository;

    public InvitesController(IInviteLinkRepository inviteLinkRepository, IChatRepository chatRepository)
    {
        _inviteLinkRepository = inviteLinkRepository;
        _chatRepository = chatRepository;
    }

    [HttpPost]
    public async Task<ActionResult<InviteLinkDto>> CreateInviteLink([FromBody] CreateInviteLinkDto createInviteLinkDto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        
        // Check if user has permission to create invite links for this chat
        var chat = await _chatRepository.GetByIdAsync(createInviteLinkDto.ChatId);
        if (chat == null)
            return NotFound("Chat not found");
            
        var isUserInChat = await _chatRepository.IsUserInChatAsync(createInviteLinkDto.ChatId, userId);
        if (!isUserInChat)
            return Forbid("You are not a member of this chat");
        
        var inviteLink = await _inviteLinkRepository.CreateAsync(createInviteLinkDto, userId);
        
        return Ok(MapToInviteLinkDto(inviteLink));
    }

    [HttpGet("{code}")]
    [AllowAnonymous]
    public async Task<ActionResult<InviteLinkInfoDto>> GetInviteLinkInfo(string code)
    {
        var inviteLink = await _inviteLinkRepository.GetByCodeAsync(code);
        if (inviteLink == null)
            return NotFound("Invite link not found");

        var isExpired = inviteLink.ExpiresAt <= DateTime.UtcNow;
        var isUsageLimitReached = inviteLink.UsageLimit.HasValue && inviteLink.UsedCount >= inviteLink.UsageLimit.Value;
        
        bool isUserAlreadyMember = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
            isUserAlreadyMember = await _chatRepository.IsUserInChatAsync(inviteLink.ChatId, userId);
        }

        var inviteLinkInfo = new InviteLinkInfoDto
        {
            InviteCode = inviteLink.InviteCode,
            ChatName = inviteLink.Chat.Name,
            ChatDescription = inviteLink.Chat.Description ?? string.Empty,
            MemberCount = inviteLink.Chat.ChatUsers.Count,
            IsExpired = isExpired,
            IsUsageLimitReached = isUsageLimitReached,
            IsActive = inviteLink.IsActive,
            IsUserAlreadyMember = isUserAlreadyMember
        };

        return Ok(inviteLinkInfo);
    }

    [HttpPost("{code}/join")]
    public async Task<ActionResult<ChatDto>> JoinChatViaInvite(string code)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        
        try
        {
            var chatUser = await _inviteLinkRepository.UseInviteLinkAsync(code, userId);
            var chat = await _chatRepository.GetByIdAsync(chatUser.ChatId);
            
            if (chat == null)
                return NotFound("Chat not found");
                
            return Ok(MapToChatDto(chat));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("chat/{chatId}")]
    public async Task<ActionResult<IEnumerable<InviteLinkDto>>> GetChatInviteLinks(Guid chatId)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        
        var isUserInChat = await _chatRepository.IsUserInChatAsync(chatId, userId);
        if (!isUserInChat)
            return Forbid("You are not a member of this chat");
        
        var inviteLinks = await _inviteLinkRepository.GetChatInviteLinksAsync(chatId);
        
        return Ok(inviteLinks.Select(MapToInviteLinkDto));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteInviteLink(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        
        var inviteLink = await _inviteLinkRepository.GetByIdAsync(id);
        if (inviteLink == null)
            return NotFound("Invite link not found");
            
        var isUserInChat = await _chatRepository.IsUserInChatAsync(inviteLink.ChatId, userId);
        if (!isUserInChat)
            return Forbid("You are not a member of this chat");
        
        await _inviteLinkRepository.DeleteAsync(id);
        
        return NoContent();
    }

    [HttpPut("{id}/deactivate")]
    public async Task<ActionResult> DeactivateInviteLink(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        
        var inviteLink = await _inviteLinkRepository.GetByIdAsync(id);
        if (inviteLink == null)
            return NotFound("Invite link not found");
            
        var isUserInChat = await _chatRepository.IsUserInChatAsync(inviteLink.ChatId, userId);
        if (!isUserInChat)
            return Forbid("You are not a member of this chat");
        
        await _inviteLinkRepository.DeactivateAsync(id);
        
        return NoContent();
    }

    private static InviteLinkDto MapToInviteLinkDto(Domain.Entities.InviteLink inviteLink)
    {
        return new InviteLinkDto
        {
            Id = inviteLink.Id,
            ChatId = inviteLink.ChatId,
            ChatName = inviteLink.Chat?.Name ?? string.Empty,
            InviteCode = inviteLink.InviteCode,
            ExpiresAt = inviteLink.ExpiresAt,
            UsageLimit = inviteLink.UsageLimit,
            UsedCount = inviteLink.UsedCount,
            IsActive = inviteLink.IsActive,
            CreatedAt = inviteLink.CreatedAt,
            CreatedBy = new UserDto
            {
                Id = inviteLink.CreatedBy.Id,
                Username = inviteLink.CreatedBy.Username,
                Email = inviteLink.CreatedBy.Email,
                DisplayName = inviteLink.CreatedBy.DisplayName,
                AvatarUrl = inviteLink.CreatedBy.AvatarUrl,
                IsOnline = inviteLink.CreatedBy.IsOnline,
                LastSeenAt = inviteLink.CreatedBy.LastSeenAt
            }
        };
    }

    private static ChatDto MapToChatDto(Domain.Entities.Chat chat)
    {
        return new ChatDto
        {
            Id = chat.Id,
            Name = chat.Name,
            Description = chat.Description,
            Type = chat.Type,
            CreatedAt = chat.CreatedAt,
            LastActivityAt = chat.LastActivityAt,
            CreatedBy = new UserDto
            {
                Id = chat.CreatedBy.Id,
                Username = chat.CreatedBy.Username,
                Email = chat.CreatedBy.Email,
                DisplayName = chat.CreatedBy.DisplayName,
                AvatarUrl = chat.CreatedBy.AvatarUrl,
                IsOnline = chat.CreatedBy.IsOnline,
                LastSeenAt = chat.CreatedBy.LastSeenAt
            },
            Users = chat.ChatUsers?.Select(cu => new ChatUserDto
            {
                Id = cu.Id,
                Role = cu.Role,
                JoinedAt = cu.JoinedAt,
                LastReadAt = cu.LastReadAt,
                User = new UserDto
                {
                    Id = cu.User.Id,
                    Username = cu.User.Username,
                    Email = cu.User.Email,
                    DisplayName = cu.User.DisplayName,
                    AvatarUrl = cu.User.AvatarUrl,
                    IsOnline = cu.User.IsOnline,
                    LastSeenAt = cu.User.LastSeenAt
                }
            }).ToList() ?? new List<ChatUserDto>(),
            LastMessage = chat.Messages?.OrderByDescending(m => m.CreatedAt).FirstOrDefault() != null
                ? new MessageDto
                {
                    Id = chat.Messages.OrderByDescending(m => m.CreatedAt).First().Id,
                    ChatId = chat.Messages.OrderByDescending(m => m.CreatedAt).First().ChatId,
                    Content = chat.Messages.OrderByDescending(m => m.CreatedAt).First().Content,
                    Type = chat.Messages.OrderByDescending(m => m.CreatedAt).First().Type,
                    CreatedAt = chat.Messages.OrderByDescending(m => m.CreatedAt).First().CreatedAt,
                    User = new UserDto
                    {
                        Id = chat.Messages.OrderByDescending(m => m.CreatedAt).First().User.Id,
                        Username = chat.Messages.OrderByDescending(m => m.CreatedAt).First().User.Username,
                        Email = chat.Messages.OrderByDescending(m => m.CreatedAt).First().User.Email,
                        DisplayName = chat.Messages.OrderByDescending(m => m.CreatedAt).First().User.DisplayName,
                        AvatarUrl = chat.Messages.OrderByDescending(m => m.CreatedAt).First().User.AvatarUrl,
                        IsOnline = chat.Messages.OrderByDescending(m => m.CreatedAt).First().User.IsOnline,
                        LastSeenAt = chat.Messages.OrderByDescending(m => m.CreatedAt).First().User.LastSeenAt
                    }
                }
                : null
        };
    }
}