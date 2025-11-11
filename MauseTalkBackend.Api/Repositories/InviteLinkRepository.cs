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
            .Where(i => i.ChatId == chatId && i.IsActive)
            .Include(i => i.CreatedBy)
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

    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}