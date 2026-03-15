namespace Cutline.Core.Entities;

public class Player
{
    public Guid Id { get; set; }
    public string? GsisId { get; set; }  // canonical ID — null until backfilled from nflverse
    public string? SleeperId { get; set; }
    public string? EspnId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string? NflTeam { get; set; }
    public PlayerStatus Status { get; set; }
    public int? ByeWeek { get; set; }

    // Metadata
    public int? Age { get; set; }
    public string? College { get; set; }
    public string? Height { get; set; }    // e.g. "6'2\""
    public string? Weight { get; set; }    // e.g. "215"
    public int? JerseyNumber { get; set; }
    public int? YearsExperience { get; set; }
    public int? DepthChartOrder { get; set; }
    public decimal? Adp { get; set; }   // Average Draft Position (from Sleeper search_rank)

    public DateTimeOffset LastSyncedAt { get; set; }
}

public enum PlayerStatus { Active, Injured, InjuredReserve, Inactive, Unknown }
