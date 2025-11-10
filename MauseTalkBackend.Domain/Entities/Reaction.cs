namespace MauseTalkBackend.Domain.Entities;

public class Reaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public ReactionType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Message Message { get; set; } = null!;
    public User User { get; set; } = null!;
}

public enum ReactionType
{
    Like = 0,
    Love = 1,
    Laugh = 2,
    Sad = 3,
    Angry = 4,
    Wow = 5
}