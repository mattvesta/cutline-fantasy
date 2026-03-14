namespace Cutline.Core.Entities;

public class ScoringSettings
{
    public decimal PassingYardsPerPoint { get; set; } = 25m;
    public decimal PassingTdPoints { get; set; } = 4m;
    public decimal RushingYardsPerPoint { get; set; } = 10m;
    public decimal RushingTdPoints { get; set; } = 6m;
    public decimal ReceivingYardsPerPoint { get; set; } = 10m;
    public decimal ReceivingTdPoints { get; set; } = 6m;
    public decimal ReceptionPoints { get; set; } = 1m;  // 0 = standard, 0.5 = half PPR, 1 = PPR
    public decimal InterceptionPoints { get; set; } = -2m;
    public decimal FumbleLostPoints { get; set; } = -2m;
}
