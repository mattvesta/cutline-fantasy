namespace Cutline.Core.Entities;

public class LeagueManager
{
    public Guid LeagueId { get; set; }
    public League League { get; set; } = null!;

    public Guid ManagerId { get; set; }
    public Manager Manager { get; set; } = null!;

    public bool IsCommissioner { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Convenience nav — the manager's team in this league (null if unassigned)
    public Team? Team => Manager.Teams.FirstOrDefault(t => t.LeagueId == LeagueId);
}
