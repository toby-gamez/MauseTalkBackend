using MauseTalkBackend.Domain.Entities;

namespace MauseTalkBackend.Domain.DTOs;

public class InviteLinkDto
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public string ChatName { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserDto CreatedBy { get; set; } = null!;
}

public class CreateInviteLinkDto
{
    public Guid ChatId { get; set; }
    public DateTime? ExpiresAt { get; set; } // null = 7 days from now
    public int? UsageLimit { get; set; } // null = unlimited
}

public class InviteLinkInfoDto
{
    public string InviteCode { get; set; } = string.Empty;
    public string ChatName { get; set; } = string.Empty;
    public string ChatDescription { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public bool IsExpired { get; set; }
    public bool IsUsageLimitReached { get; set; }
    public bool IsActive { get; set; }
    public bool IsUserAlreadyMember { get; set; }
}