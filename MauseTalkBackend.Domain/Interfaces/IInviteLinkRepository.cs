using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Entities;

namespace MauseTalkBackend.Domain.Interfaces;

public interface IInviteLinkRepository
{
    Task<InviteLink?> GetByIdAsync(Guid id);
    Task<InviteLink?> GetByCodeAsync(string inviteCode);
    Task<IEnumerable<InviteLink>> GetChatInviteLinksAsync(Guid chatId);
    Task<InviteLink> CreateAsync(CreateInviteLinkDto createInviteLinkDto, Guid createdById);
    Task<InviteLink> UpdateAsync(InviteLink inviteLink);
    Task DeleteAsync(Guid id);
    Task<bool> IsValidAsync(string inviteCode);
    Task<ChatUser> UseInviteLinkAsync(string inviteCode, Guid userId);
    Task DeactivateAsync(Guid id);
}