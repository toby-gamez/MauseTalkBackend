using MauseTalkBackend.Domain.Entities;

namespace MauseTalkBackend.Domain.DTOs;

public class ChatDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public ChatType Type { get; set; }
    public UserDto CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public IEnumerable<ChatUserDto> Users { get; set; } = new List<ChatUserDto>();
    public MessageDto? LastMessage { get; set; }
    
    // Chat Settings
    public bool AllowInvites { get; set; } = true;
    public bool AllowMembersToInvite { get; set; } = true;
    public int MaxMembers { get; set; } = 100;
}

public class CreateChatDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public ChatType Type { get; set; } = ChatType.Group;
    public IEnumerable<Guid> UserIds { get; set; } = new List<Guid>();
    
    // Chat Settings
    public bool AllowInvites { get; set; } = true;
    public bool AllowMembersToInvite { get; set; } = true;
    public int MaxMembers { get; set; } = 100;
}

public class ChatUserDto
{
    public Guid Id { get; set; }
    public UserDto User { get; set; } = null!;
    public ChatUserRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LastReadAt { get; set; }
}

public class UpdateChatDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public bool? AllowInvites { get; set; }
    public bool? AllowMembersToInvite { get; set; }
    public int? MaxMembers { get; set; }
}

public class UpdateChatUserRoleDto
{
    public Guid UserId { get; set; }
    public ChatUserRole Role { get; set; }
}