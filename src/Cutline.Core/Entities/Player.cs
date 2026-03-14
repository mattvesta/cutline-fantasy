namespace Cutline.Core.Entities;

public class Player
{
    public Guid Id { get; set; }
    public string GsisId { get; set; } = string.Empty;  // canonical ID
    public string? SleeperId { get; set; }
    public string? EspnId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string? NflTeam { get; set; }
    public PlayerStatus Status { get; set; }
    public int? ByeWeek { get; set; }
    public DateTimeOffset LastSyncedAt { get; set; }
}

public enum PlayerStatus { Active, Injured, InjuredReserve, Inactive, Unknown }
