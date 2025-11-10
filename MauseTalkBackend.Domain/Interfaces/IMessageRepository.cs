using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Entities;

namespace MauseTalkBackend.Domain.Interfaces;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid id);
    Task<IEnumerable<Message>> GetChatMessagesAsync(Guid chatId, int skip = 0, int take = 50);
    Task<Message> CreateAsync(CreateMessageDto createMessageDto, Guid userId);
    Task<Message> UpdateAsync(Message message);
    Task DeleteAsync(Guid id);
    Task<Reaction> AddReactionAsync(Guid messageId, Guid userId, ReactionType type);
    Task RemoveReactionAsync(Guid messageId, Guid userId, ReactionType type);
    Task<IEnumerable<Reaction>> GetMessageReactionsAsync(Guid messageId);
}