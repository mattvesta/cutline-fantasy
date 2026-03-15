namespace Cutline.Core.Entities;

public class Trade
{
    public Guid Id { get; set; }
    public Guid LeagueId { get; set; }

    public Guid InitiatorTeamId { get; set; }
    public Team InitiatorTeam { get; set; } = null!;

    public Guid ReceiverTeamId { get; set; }
    public Team ReceiverTeam { get; set; } = null!;

    public TradeStatus Status { get; set; } = TradeStatus.Pending;

    /// <summary>Optional note from the proposing manager.</summary>
    public string? Message { get; set; }

    public DateTime ProposedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public ICollection<TradeItem> Items { get; set; } = [];
}

public enum TradeStatus { Pending, Accepted, Rejected, Cancelled }
