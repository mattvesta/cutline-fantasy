namespace Cutline.Core.Entities;

public class Team
{
    public Guid Id { get; set; }
    public Guid LeagueId { get; set; }
    public League League { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;  // legacy display name
    public Guid? ManagerId { get; set; }
    public Manager? Manager { get; set; }
    public bool IsEliminated { get; set; }
    public int? EliminatedWeek { get; set; }
    /// <summary>Commissioner-controlled lock. Prevents lineup changes, waiver claims, drops, and trades.</summary>
    public bool IsLocked { get; set; }
    public ICollection<RosterSlot> RosterSlots { get; set; } = new List<RosterSlot>();
}
