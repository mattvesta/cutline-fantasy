namespace Cutline.Core.Entities;

public class League
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Season { get; set; }
    public LeagueStatus Status { get; set; }
    public ScoringSettings ScoringSettings { get; set; } = new();
    public RosterSettings RosterSettings { get; set; } = new();
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<Week> Weeks { get; set; } = new List<Week>();
    public ICollection<LeagueManager> LeagueManagers { get; set; } = new List<LeagueManager>();
}

public enum LeagueStatus { Setup, Drafting, Active, Completed }
