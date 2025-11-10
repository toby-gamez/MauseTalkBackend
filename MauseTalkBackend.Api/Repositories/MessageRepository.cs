using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Entities;
using MauseTalkBackend.Domain.Interfaces;
using MauseTalkBackend.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MauseTalkBackend.Api.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly MauseTalkDbContext _context;

    public MessageRepository(MauseTalkDbContext context)
    {
        _context = context;
    }

    public async Task<Message?> GetByIdAsync(Guid id)
    {
        return await _context.Messages
            .Include(m => m.User)
            .Include(m => m.Reactions)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<Message>> GetChatMessagesAsync(Guid chatId, int skip = 0, int take = 50)
    {
        return await _context.Messages
            .Where(m => m.ChatId == chatId && !m.IsDeleted)
            .Include(m => m.User)
            .Include(m => m.Reactions)
                .ThenInclude(r => r.User)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Message> CreateAsync(CreateMessageDto createMessageDto, Guid userId)
    {
        var message = new Message
        {
            ChatId = createMessageDto.ChatId,
            UserId = userId,
            Content = createMessageDto.Content,
            Type = createMessageDto.Type,
            FileUrl = createMessageDto.FileUrl,
            FileName = createMessageDto.FileName,
            FileSize = createMessageDto.FileSize,
            MimeType = createMessageDto.MimeType,
            CreatedAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        
        // Update chat last activity
        var chat = await _context.Chats.FindAsync(createMessageDto.ChatId);
        if (chat != null)
        {
            chat.LastActivityAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        
        return await GetByIdAsync(message.Id) ?? message;
    }

    public async Task<Message> UpdateAsync(Message message)
    {
        message.EditedAt = DateTime.UtcNow;
        _context.Messages.Update(message);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(message.Id) ?? message;
    }

    public async Task DeleteAsync(Guid id)
    {
        var message = await _context.Messages.FindAsync(id);
        if (message != null)
        {
            message.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Reaction> AddReactionAsync(Guid messageId, Guid userId, ReactionType type)
    {
        // Remove existing reaction of the same type if exists
        var existingReaction = await _context.Reactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId && r.Type == type);

        if (existingReaction != null)
        {
            return existingReaction;
        }

        var reaction = new Reaction
        {
            MessageId = messageId,
            UserId = userId,
            Type = type,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reactions.Add(reaction);
        await _context.SaveChangesAsync();
        return reaction;
    }

    public async Task RemoveReactionAsync(Guid messageId, Guid userId, ReactionType type)
    {
        var reaction = await _context.Reactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId && r.Type == type);

        if (reaction != null)
        {
            _context.Reactions.Remove(reaction);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Reaction>> GetMessageReactionsAsync(Guid messageId)
    {
        return await _context.Reactions
            .Where(r => r.MessageId == messageId)
            .Include(r => r.User)
            .ToListAsync();
    }
}