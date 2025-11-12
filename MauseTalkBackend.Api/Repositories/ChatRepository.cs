using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Entities;
using MauseTalkBackend.Domain.Interfaces;
using MauseTalkBackend.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MauseTalkBackend.Api.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly MauseTalkDbContext _context;

    public ChatRepository(MauseTalkDbContext context)
    {
        _context = context;
    }

    public async Task<Chat?> GetByIdAsync(Guid id)
    {
        return await _context.Chats
            .Include(c => c.CreatedBy)
            .Include(c => c.ChatUsers)
                .ThenInclude(cu => cu.User)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Chat>> GetUserChatsAsync(Guid userId)
    {
        return await _context.Chats
            .Where(c => c.ChatUsers.Any(cu => cu.UserId == userId))
            .Include(c => c.CreatedBy)
            .Include(c => c.ChatUsers)
                .ThenInclude(cu => cu.User)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .ThenInclude(m => m.User)
            .OrderByDescending(c => c.LastActivityAt)
            .ToListAsync();
    }

    public async Task<Chat> CreateAsync(CreateChatDto createChatDto, Guid createdById)
    {
        var chat = new Chat
        {
            Name = createChatDto.Name,
            Description = createChatDto.Description,
            AvatarUrl = createChatDto.AvatarUrl,
            Type = createChatDto.Type,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            AllowInvites = createChatDto.AllowInvites,
            AllowMembersToInvite = createChatDto.AllowMembersToInvite,
            MaxMembers = createChatDto.MaxMembers
        };

        _context.Chats.Add(chat);
        await _context.SaveChangesAsync();

        // Add creator as owner
        await AddUserToChatAsync(chat.Id, createdById, ChatUserRole.Owner);

        // Add other users
        foreach (var userId in createChatDto.UserIds)
        {
            if (userId != createdById)
            {
                await AddUserToChatAsync(chat.Id, userId, ChatUserRole.Member);
            }
        }

        return await GetByIdAsync(chat.Id) ?? chat;
    }

    public async Task<Chat> UpdateAsync(Chat chat)
    {
        _context.Chats.Update(chat);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(chat.Id) ?? chat;
    }

    public async Task DeleteAsync(Guid id)
    {
        var chat = await _context.Chats.FindAsync(id);
        if (chat != null)
        {
            _context.Chats.Remove(chat);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<ChatUser> AddUserToChatAsync(Guid chatId, Guid userId, ChatUserRole role = ChatUserRole.Member)
    {
        var existingChatUser = await _context.ChatUsers
            .FirstOrDefaultAsync(cu => cu.ChatId == chatId && cu.UserId == userId);

        if (existingChatUser != null)
        {
            return existingChatUser;
        }

        var chatUser = new ChatUser
        {
            ChatId = chatId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };

        _context.ChatUsers.Add(chatUser);
        await _context.SaveChangesAsync();
        return chatUser;
    }

    public async Task RemoveUserFromChatAsync(Guid chatId, Guid userId)
    {
        var chatUser = await _context.ChatUsers
            .FirstOrDefaultAsync(cu => cu.ChatId == chatId && cu.UserId == userId);

        if (chatUser != null)
        {
            _context.ChatUsers.Remove(chatUser);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<ChatUser>> GetChatUsersAsync(Guid chatId)
    {
        return await _context.ChatUsers
            .Where(cu => cu.ChatId == chatId)
            .Include(cu => cu.User)
            .ToListAsync();
    }

    public async Task UpdateLastReadAsync(Guid chatId, Guid userId)
    {
        var chatUser = await _context.ChatUsers
            .FirstOrDefaultAsync(cu => cu.ChatId == chatId && cu.UserId == userId);

        if (chatUser != null)
        {
            chatUser.LastReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsUserInChatAsync(Guid chatId, Guid userId)
    {
        return await _context.ChatUsers
            .AnyAsync(cu => cu.ChatId == chatId && cu.UserId == userId);
    }

    public async Task UpdateLastActivityAsync(Guid chatId)
    {
        var chat = await _context.Chats.FindAsync(chatId);
        if (chat != null)
        {
            chat.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> HasUserRole(Guid chatId, Guid userId, ChatUserRole minimumRole)
    {
        var chatUser = await _context.ChatUsers
            .FirstOrDefaultAsync(cu => cu.ChatId == chatId && cu.UserId == userId);
        
        return chatUser != null && chatUser.Role >= minimumRole;
    }

    public async Task<Chat> UpdateChatSettingsAsync(Guid chatId, UpdateChatDto updateDto)
    {
        var chat = await _context.Chats.FindAsync(chatId);
        if (chat == null)
            throw new ArgumentException("Chat not found");

        if (!string.IsNullOrEmpty(updateDto.Name))
            chat.Name = updateDto.Name;
        
        if (updateDto.Description is not null)
            chat.Description = updateDto.Description;
        
        if (updateDto.AvatarUrl is not null)
            chat.AvatarUrl = updateDto.AvatarUrl;
        
        if (updateDto.AllowInvites.HasValue)
            chat.AllowInvites = updateDto.AllowInvites.Value;
        
        if (updateDto.AllowMembersToInvite.HasValue)
            chat.AllowMembersToInvite = updateDto.AllowMembersToInvite.Value;
        
        if (updateDto.MaxMembers.HasValue)
            chat.MaxMembers = updateDto.MaxMembers.Value;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(chatId) ?? chat;
    }

    public async Task<ChatUser> UpdateUserRoleAsync(Guid chatId, Guid userId, ChatUserRole newRole)
    {
        var chatUser = await _context.ChatUsers
            .FirstOrDefaultAsync(cu => cu.ChatId == chatId && cu.UserId == userId);
        
        if (chatUser == null)
            throw new ArgumentException("User not in chat");

        chatUser.Role = newRole;
        await _context.SaveChangesAsync();
        return chatUser;
    }
}