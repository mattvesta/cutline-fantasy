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
}

public enum WeekStatus { Upcoming, InProgress, Scoring, Eliminated, Completed }
