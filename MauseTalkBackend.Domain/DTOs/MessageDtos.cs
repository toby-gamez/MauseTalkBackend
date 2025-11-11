using MauseTalkBackend.Domain.Entities;

namespace MauseTalkBackend.Domain.DTOs;

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public UserDto User { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public IEnumerable<ReactionDto> Reactions { get; set; } = new List<ReactionDto>();
}

public class CreateMessageDto
{
    public Guid ChatId { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
}

public class ReactionDto
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
    public ReactionType Type { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReactionDto
{
    public Guid MessageId { get; set; }
    public ReactionType Type { get; set; }
}