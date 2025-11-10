using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Entities;

namespace MauseTalkBackend.Domain.Interfaces;

public interface IChatRepository
{
    Task<Chat?> GetByIdAsync(Guid id);
    Task<IEnumerable<Chat>> GetUserChatsAsync(Guid userId);
    Task<Chat> CreateAsync(CreateChatDto createChatDto, Guid createdById);
    Task<Chat> UpdateAsync(Chat chat);
    Task DeleteAsync(Guid id);
    Task<ChatUser> AddUserToChatAsync(Guid chatId, Guid userId, ChatUserRole role = ChatUserRole.Member);
    Task RemoveUserFromChatAsync(Guid chatId, Guid userId);
    Task<IEnumerable<ChatUser>> GetChatUsersAsync(Guid chatId);
    Task UpdateLastReadAsync(Guid chatId, Guid userId);
    Task<bool> IsUserInChatAsync(Guid chatId, Guid userId);
    Task UpdateLastActivityAsync(Guid chatId);
}