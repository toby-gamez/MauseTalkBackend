namespace MauseTalkBackend.Domain.Entities;

public class InviteLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChatId { get; set; }
    public Guid CreatedById { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int? UsageLimit { get; set; } // null = unlimited
    public int UsedCount { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Chat Chat { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
}