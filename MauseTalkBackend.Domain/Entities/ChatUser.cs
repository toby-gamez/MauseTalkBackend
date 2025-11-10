namespace MauseTalkBackend.Domain.Entities;

public class ChatUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChatId { get; set; }
    public Guid UserId { get; set; }
    public ChatUserRole Role { get; set; } = ChatUserRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastReadAt { get; set; }
    
    // Navigation properties
    public Chat Chat { get; set; } = null!;
    public User User { get; set; } = null!;
}

public enum ChatUserRole
{
    Member = 0,
    Admin = 1,
    Owner = 2
}