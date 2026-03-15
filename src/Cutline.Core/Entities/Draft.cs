namespace Cutline.Core.Entities;

public class Draft
{
    public Guid Id { get; set; }
    public Guid LeagueId { get; set; }
    public League League { get; set; } = null!;
    public DraftStatus Status { get; set; } = DraftStatus.Pending;
    public int PickTimeLimitSeconds { get; set; } = 90;
    public int CurrentPickNumber { get; set; } = 1; // 1-based overall pick number
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CurrentPickStartedAt { get; set; }
    public ICollection<DraftPick> Picks { get; set; } = new List<DraftPick>();
}

public enum DraftStatus { Pending, InProgress, Paused, Completed }
