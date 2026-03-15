namespace Cutline.Core.Entities;

public class TradeItem
{
    public Guid Id { get; set; }
    public Guid TradeId { get; set; }
    public Trade Trade { get; set; } = null!;

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    /// <summary>The team that is giving this player away in the trade.</summary>
    public Guid FromTeamId { get; set; }
}
