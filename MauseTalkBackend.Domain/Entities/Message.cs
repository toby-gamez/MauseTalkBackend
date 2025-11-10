namespace MauseTalkBackend.Domain.Entities;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChatId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation properties
    public Chat Chat { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
}

public enum MessageType
{
    Text = 0,
    Image = 1,
    Voice = 2,
    File = 3,
    System = 4
}