using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Interfaces;
using MauseTalkBackend.Shared.Constants;
using MauseTalkBackend.Shared.Extensions;
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
        var userId = User.GetUserId();
        
        // Check if user has permission to create invite links for this chat
        var chat = await _chatRepository.GetByIdAsync(createInviteLinkDto.ChatId);
        if (chat == null)
            return NotFound("Chat not found");
            
        var isUserInChat = await _chatRepository.IsUserInChatAsync(createInviteLinkDto.ChatId, userId);
        if (!isUserInChat)
            return Forbid("You are not a member of this chat");
            
        // Check chat settings
        if (!chat.AllowInvites)
            return Forbid("Invite links are disabled for this chat");
            
        // Check if user can create invite links
        var isAdmin = await _chatRepository.HasUserRole(createInviteLinkDto.ChatId, userId, MauseTalkBackend.Domain.Entities.ChatUserRole.Admin);
        if (!chat.AllowMembersToInvite && !isAdmin)
            return Forbid("Only admins can create invite links for this chat");
        
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
            try
            {
                var userId = User.GetUserId();
                isUserAlreadyMember = await _chatRepository.IsUserInChatAsync(inviteLink.ChatId, userId);
            }
            catch (UnauthorizedAccessException)
            {
                // User claims are not valid, treat as not authenticated
            }
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
        var userId = User.GetUserId();
        
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
        var userId = User.GetUserId();
        
        var isUserInChat = await _chatRepository.IsUserInChatAsync(chatId, userId);
        if (!isUserInChat)
            return Forbid("You are not a member of this chat");
        
        // Admins/owners can see all invite links, members see only active ones
        var isAdmin = await _chatRepository.HasUserRole(chatId, userId, MauseTalkBackend.Domain.Entities.ChatUserRole.Admin);
        
        var inviteLinks = isAdmin 
            ? await _inviteLinkRepository.GetAllChatInviteLinksAsync(chatId)
            : await _inviteLinkRepository.GetChatInviteLinksAsync(chatId);
        
        return Ok(inviteLinks.Select(MapToInviteLinkDto));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteInviteLink(Guid id)
    {
        var userId = User.GetUserId();
        
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
        var userId = User.GetUserId();
        
        var inviteLink = await _inviteLinkRepository.GetByIdAsync(id);
        if (inviteLink == null)
            return NotFound("Invite link not found");
            
        var isUserInChat = await _chatRepository.IsUserInChatAsync(inviteLink.ChatId, userId);
        if (!isUserInChat)
            return Forbid("You are not a member of this chat");
        
        await _inviteLinkRepository.DeactivateAsync(id);
        
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<InviteLinkDto>> UpdateInviteLink(Guid id, [FromBody] UpdateInviteLinkDto updateDto)
    {
        var userId = User.GetUserId();
        
        var inviteLink = await _inviteLinkRepository.GetByIdAsync(id);
        if (inviteLink == null)
            return NotFound("Invite link not found");
            
        // Only admins/owners can update invite links
        var isAdmin = await _chatRepository.HasUserRole(inviteLink.ChatId, userId, MauseTalkBackend.Domain.Entities.ChatUserRole.Admin);
        if (!isAdmin)
            return Forbid("You don't have permission to update invite links");
        
        var updatedInviteLink = await _inviteLinkRepository.UpdateAsync(id, updateDto);
        
        return Ok(MapToInviteLinkDto(updatedInviteLink));
    }

    [HttpPut("{id}/suspend")]
    public async Task<ActionResult<InviteLinkDto>> SuspendInviteLink(Guid id, [FromBody] SuspendInviteLinkDto suspendDto)
    {
        var userId = User.GetUserId();
        
        var inviteLink = await _inviteLinkRepository.GetByIdAsync(id);
        if (inviteLink == null)
            return NotFound("Invite link not found");
            
        // Only admins/owners can suspend invite links
        var isAdmin = await _chatRepository.HasUserRole(inviteLink.ChatId, userId, MauseTalkBackend.Domain.Entities.ChatUserRole.Admin);
        if (!isAdmin)
            return Forbid("You don't have permission to suspend invite links");
        
        var suspendedInviteLink = await _inviteLinkRepository.SuspendAsync(id, userId, suspendDto.Reason);
        
        return Ok(MapToInviteLinkDto(suspendedInviteLink));
    }

    [HttpPut("{id}/unsuspend")]
    public async Task<ActionResult<InviteLinkDto>> UnsuspendInviteLink(Guid id)
    {
        var userId = User.GetUserId();
        
        var inviteLink = await _inviteLinkRepository.GetByIdAsync(id);
        if (inviteLink == null)
            return NotFound("Invite link not found");
            
        // Only admins/owners can unsuspend invite links
        var isAdmin = await _chatRepository.HasUserRole(inviteLink.ChatId, userId, MauseTalkBackend.Domain.Entities.ChatUserRole.Admin);
        if (!isAdmin)
            return Forbid("You don't have permission to unsuspend invite links");
        
        var unsuspendedInviteLink = await _inviteLinkRepository.UnsuspendAsync(id);
        
        return Ok(MapToInviteLinkDto(unsuspendedInviteLink));
    }

    [HttpPut("{id}/block")]
    public async Task<ActionResult<InviteLinkDto>> BlockInviteLink(Guid id)
    {
        var userId = User.GetUserId();
        
        var inviteLink = await _inviteLinkRepository.GetByIdAsync(id);
        if (inviteLink == null)
            return NotFound("Invite link not found");
            
        // Only owners can block invite links
        var isOwner = await _chatRepository.HasUserRole(inviteLink.ChatId, userId, MauseTalkBackend.Domain.Entities.ChatUserRole.Owner);
        if (!isOwner)
            return Forbid("You don't have permission to block invite links");
        
        var blockedInviteLink = await _inviteLinkRepository.BlockAsync(id, userId);
        
        return Ok(MapToInviteLinkDto(blockedInviteLink));
    }

    [HttpPut("{id}/unblock")]
    public async Task<ActionResult<InviteLinkDto>> UnblockInviteLink(Guid id)
    {
        var userId = User.GetUserId();
        
        var inviteLink = await _inviteLinkRepository.GetByIdAsync(id);
        if (inviteLink == null)
            return NotFound("Invite link not found");
            
        // Only owners can unblock invite links
        var isOwner = await _chatRepository.HasUserRole(inviteLink.ChatId, userId, MauseTalkBackend.Domain.Entities.ChatUserRole.Owner);
        if (!isOwner)
            return Forbid("You don't have permission to unblock invite links");
        
        var unblockedInviteLink = await _inviteLinkRepository.UnblockAsync(id);
        
        return Ok(MapToInviteLinkDto(unblockedInviteLink));
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
            IsSuspended = inviteLink.IsSuspended,
            IsBlocked = inviteLink.IsBlocked,
            SuspensionReason = inviteLink.SuspensionReason,
            SuspendedAt = inviteLink.SuspendedAt,
            SuspendedBy = inviteLink.SuspendedBy != null ? new UserDto
            {
                Id = inviteLink.SuspendedBy.Id,
                Username = inviteLink.SuspendedBy.Username,
                Email = inviteLink.SuspendedBy.Email,
                DisplayName = inviteLink.SuspendedBy.DisplayName,
                AvatarUrl = inviteLink.SuspendedBy.AvatarUrl,
                IsOnline = inviteLink.SuspendedBy.IsOnline,
                LastSeenAt = inviteLink.SuspendedBy.LastSeenAt
            } : null,
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
            AvatarUrl = chat.AvatarUrl,
            Type = chat.Type,
            CreatedAt = chat.CreatedAt,
            LastActivityAt = chat.LastActivityAt,
            AllowInvites = chat.AllowInvites,
            AllowMembersToInvite = chat.AllowMembersToInvite,
            MaxMembers = chat.MaxMembers,
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