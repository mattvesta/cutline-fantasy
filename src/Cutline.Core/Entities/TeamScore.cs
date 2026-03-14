namespace Cutline.Core.Entities;

public class TeamScore
{
    public Guid Id { get; set; }
    public Guid WeekId { get; set; }
    public Week Week { get; set; } = null!;
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public decimal Points { get; set; }
    public bool IsLocked { get; set; }  // true once reconciled against nflverse final stats
}
