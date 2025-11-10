namespace MauseTalkBackend.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public bool IsOnline { get; set; }
    
    // Navigation properties
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
    public ICollection<ChatUser> ChatUsers { get; set; } = new List<ChatUser>();
}