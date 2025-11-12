using MauseTalkBackend.Api.Data;
using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Entities;
using MauseTalkBackend.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MauseTalkBackend.Api.Repositories;

public class InviteLinkRepository : IInviteLinkRepository
{
    private readonly MauseTalkDbContext _context;

    public InviteLinkRepository(MauseTalkDbContext context)
    {
        _context = context;
    }

    public async Task<InviteLink?> GetByIdAsync(Guid id)
    {
        return await _context.InviteLinks
            .Include(i => i.Chat)
            .Include(i => i.CreatedBy)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<InviteLink?> GetByCodeAsync(string inviteCode)
    {
        return await _context.InviteLinks
            .Include(i => i.Chat)
                .ThenInclude(c => c.ChatUsers)
            .Include(i => i.CreatedBy)
            .FirstOrDefaultAsync(i => i.InviteCode == inviteCode);
    }

    public async Task<IEnumerable<InviteLink>> GetChatInviteLinksAsync(Guid chatId)
    {
        return await _context.InviteLinks
            .Where(i => i.ChatId == chatId && i.IsActive && !i.IsSuspended && !i.IsBlocked)
            .Include(i => i.CreatedBy)
            .Include(i => i.SuspendedBy)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<InviteLink>> GetAllChatInviteLinksAsync(Guid chatId)
    {
        return await _context.InviteLinks
            .Where(i => i.ChatId == chatId)
            .Include(i => i.CreatedBy)
            .Include(i => i.SuspendedBy)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<InviteLink> CreateAsync(CreateInviteLinkDto createInviteLinkDto, Guid createdById)
    {
        var inviteLink = new InviteLink
        {
            ChatId = createInviteLinkDto.ChatId,
            CreatedById = createdById,
            InviteCode = GenerateInviteCode(),
            ExpiresAt = createInviteLinkDto.ExpiresAt ?? DateTime.UtcNow.AddDays(7),
            UsageLimit = createInviteLinkDto.UsageLimit,
            CreatedAt = DateTime.UtcNow
        };

        _context.InviteLinks.Add(inviteLink);
        await _context.SaveChangesAsync();
        
        return await GetByIdAsync(inviteLink.Id) ?? inviteLink;
    }

    public async Task<InviteLink> UpdateAsync(InviteLink inviteLink)
    {
        _context.InviteLinks.Update(inviteLink);
        await _context.SaveChangesAsync();
        return inviteLink;
    }

    public async Task<InviteLink> UpdateAsync(Guid id, UpdateInviteLinkDto updateDto)
    {
        var inviteLink = await _context.InviteLinks.FindAsync(id);
        if (inviteLink == null)
            throw new ArgumentException("Invite link not found");

        if (updateDto.ExpiresAt.HasValue)
            inviteLink.ExpiresAt = updateDto.ExpiresAt.Value;
        
        if (updateDto.UsageLimit.HasValue)
            inviteLink.UsageLimit = updateDto.UsageLimit;
        
        if (updateDto.IsActive.HasValue)
            inviteLink.IsActive = updateDto.IsActive.Value;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id) ?? inviteLink;
    }

    public async Task DeleteAsync(Guid id)
    {
        var inviteLink = await _context.InviteLinks.FindAsync(id);
        if (inviteLink != null)
        {
            _context.InviteLinks.Remove(inviteLink);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsValidAsync(string inviteCode)
    {
        var inviteLink = await _context.InviteLinks
            .FirstOrDefaultAsync(i => i.InviteCode == inviteCode && i.IsActive);
        
        if (inviteLink == null)
            return false;
            
        if (inviteLink.IsSuspended || inviteLink.IsBlocked)
            return false;
            
        if (inviteLink.ExpiresAt <= DateTime.UtcNow)
            return false;
            
        if (inviteLink.UsageLimit.HasValue && inviteLink.UsedCount >= inviteLink.UsageLimit.Value)
            return false;
            
        return true;
    }

    public async Task<ChatUser> UseInviteLinkAsync(string inviteCode, Guid userId)
    {
        var inviteLink = await GetByCodeAsync(inviteCode);
        
        if (inviteLink == null || !await IsValidAsync(inviteCode))
            throw new InvalidOperationException("Invalid or expired invite link");
            
        // Check if user is already in chat
        var existingChatUser = await _context.ChatUsers
            .FirstOrDefaultAsync(cu => cu.ChatId == inviteLink.ChatId && cu.UserId == userId);
            
        if (existingChatUser != null)
            return existingChatUser;
            
        // Add user to chat
        var chatUser = new ChatUser
        {
            ChatId = inviteLink.ChatId,
            UserId = userId,
            Role = ChatUserRole.Member,
            JoinedAt = DateTime.UtcNow
        };
        
        _context.ChatUsers.Add(chatUser);
        
        // Increment usage count
        inviteLink.UsedCount++;
        _context.InviteLinks.Update(inviteLink);
        
        await _context.SaveChangesAsync();
        
        return chatUser;
    }

    public async Task DeactivateAsync(Guid id)
    {
        var inviteLink = await _context.InviteLinks.FindAsync(id);
        if (inviteLink != null)
        {
            inviteLink.IsActive = false;
            _context.InviteLinks.Update(inviteLink);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<InviteLink> SuspendAsync(Guid id, Guid suspendedById, string? reason = null)
    {
        var inviteLink = await _context.InviteLinks.FindAsync(id);
        if (inviteLink == null)
            throw new ArgumentException("Invite link not found");

        inviteLink.IsSuspended = true;
        inviteLink.SuspendedById = suspendedById;
        inviteLink.SuspendedAt = DateTime.UtcNow;
        inviteLink.SuspensionReason = reason;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id) ?? inviteLink;
    }

    public async Task<InviteLink> UnsuspendAsync(Guid id)
    {
        var inviteLink = await _context.InviteLinks.FindAsync(id);
        if (inviteLink == null)
            throw new ArgumentException("Invite link not found");

        inviteLink.IsSuspended = false;
        inviteLink.SuspendedById = null;
        inviteLink.SuspendedAt = null;
        inviteLink.SuspensionReason = null;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id) ?? inviteLink;
    }

    public async Task<InviteLink> BlockAsync(Guid id, Guid blockedById)
    {
        var inviteLink = await _context.InviteLinks.FindAsync(id);
        if (inviteLink == null)
            throw new ArgumentException("Invite link not found");

        inviteLink.IsBlocked = true;
        inviteLink.SuspendedById = blockedById;
        inviteLink.SuspendedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id) ?? inviteLink;
    }

    public async Task<InviteLink> UnblockAsync(Guid id)
    {
        var inviteLink = await _context.InviteLinks.FindAsync(id);
        if (inviteLink == null)
            throw new ArgumentException("Invite link not found");

        inviteLink.IsBlocked = false;
        if (!inviteLink.IsSuspended)
        {
            inviteLink.SuspendedById = null;
            inviteLink.SuspendedAt = null;
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id) ?? inviteLink;
    }

    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}