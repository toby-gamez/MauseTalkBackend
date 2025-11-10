using MauseTalkBackend.Domain.Entities;

namespace MauseTalkBackend.Domain.DTOs;

public class ChatDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ChatType Type { get; set; }
    public UserDto CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public IEnumerable<ChatUserDto> Users { get; set; } = new List<ChatUserDto>();
    public MessageDto? LastMessage { get; set; }
}

public class CreateChatDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ChatType Type { get; set; } = ChatType.Group;
    public IEnumerable<Guid> UserIds { get; set; } = new List<Guid>();
}

public class ChatUserDto
{
    public Guid Id { get; set; }
    public UserDto User { get; set; } = null!;
    public ChatUserRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LastReadAt { get; set; }
}