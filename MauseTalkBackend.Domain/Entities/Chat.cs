namespace MauseTalkBackend.Domain.Entities;

public class Chat
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ChatType Type { get; set; } = ChatType.Group;
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User CreatedBy { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<ChatUser> ChatUsers { get; set; } = new List<ChatUser>();
}

public enum ChatType
{
    Direct = 0,
    Group = 1
}