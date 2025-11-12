using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Entities;

namespace MauseTalkBackend.Domain.Interfaces;

public interface IInviteLinkRepository
{
    Task<InviteLink?> GetByIdAsync(Guid id);
    Task<InviteLink?> GetByCodeAsync(string inviteCode);
    Task<IEnumerable<InviteLink>> GetChatInviteLinksAsync(Guid chatId);
    Task<IEnumerable<InviteLink>> GetAllChatInviteLinksAsync(Guid chatId); // Including suspended/blocked
    Task<InviteLink> CreateAsync(CreateInviteLinkDto createInviteLinkDto, Guid createdById);
    Task<InviteLink> UpdateAsync(InviteLink inviteLink);
    Task<InviteLink> UpdateAsync(Guid id, UpdateInviteLinkDto updateDto);
    Task DeleteAsync(Guid id);
    Task<bool> IsValidAsync(string inviteCode);
    Task<ChatUser> UseInviteLinkAsync(string inviteCode, Guid userId);
    Task DeactivateAsync(Guid id);
    Task<InviteLink> SuspendAsync(Guid id, Guid suspendedById, string? reason = null);
    Task<InviteLink> UnsuspendAsync(Guid id);
    Task<InviteLink> BlockAsync(Guid id, Guid blockedById);
    Task<InviteLink> UnblockAsync(Guid id);
}