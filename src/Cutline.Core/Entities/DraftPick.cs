namespace Cutline.Core.Entities;

public class DraftPick
{
    public Guid Id { get; set; }
    public Guid DraftId { get; set; }
    public Draft Draft { get; set; } = null!;
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public Guid? PlayerId { get; set; }
    public Player? Player { get; set; }
    public int PickNumber { get; set; }   // overall sequential (1, 2, 3...)
    public int Round { get; set; }
    public int RoundPick { get; set; }    // pick within round (1-based)
    public DateTime? PickedAt { get; set; }
    public bool IsAutoPick { get; set; }
}
