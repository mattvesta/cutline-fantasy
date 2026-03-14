namespace Cutline.Core.Entities;

public class Team
{
    public Guid Id { get; set; }
    public Guid LeagueId { get; set; }
    public League League { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public bool IsEliminated { get; set; }
    public int? EliminatedWeek { get; set; }
    public ICollection<RosterSlot> RosterSlots { get; set; } = new List<RosterSlot>();
}
