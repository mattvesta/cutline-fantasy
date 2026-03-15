namespace Cutline.Core.Entities;

public class Week
{
    public Guid Id { get; set; }
    public Guid LeagueId { get; set; }
    public League League { get; set; } = null!;
    public int WeekNumber { get; set; }
    public WeekStatus Status { get; set; }
    public Guid? EliminatedTeamId { get; set; }
    public Team? EliminatedTeam { get; set; }
    public ICollection<TeamScore> TeamScores { get; set; } = new List<TeamScore>();
    public ICollection<WaiverClaim> WaiverClaims { get; set; } = new List<WaiverClaim>();
    /// <summary>Snapshot of the eliminated team's player IDs taken at the moment of elimination.</summary>
    public Guid[] DroppedPlayerIds { get; set; } = [];
}

public enum WeekStatus { Upcoming, InProgress, Scoring, Eliminated, Completed }
