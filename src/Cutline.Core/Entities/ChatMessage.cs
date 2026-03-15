namespace Cutline.Core.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid LeagueId { get; set; }
    public Guid ManagerId { get; set; }
    public Manager Manager { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    /// <summary>Direct GIF URL (Tenor/Giphy). Null for text-only messages.</summary>
    public string? GifUrl { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
