namespace Cutline.Core.Entities;

public class RosterSettings
{
    public int QbSlots { get; set; } = 1;
    public int RbSlots { get; set; } = 2;
    public int WrSlots { get; set; } = 2;
    public int TeSlots { get; set; } = 1;
    public int FlexSlots { get; set; } = 1;
    public int SuperFlexSlots { get; set; } = 0;
    public int KSlots { get; set; } = 1;
    public int DefSlots { get; set; } = 1;
    public int BenchSlots { get; set; } = 6;
    public int IrSlots { get; set; } = 1;
    public bool TePremium { get; set; } = false;
    public bool UseFaab { get; set; } = false;
    public decimal FaabBudget { get; set; } = 100m;
    /// <summary>Minimum allowed FAAB bid. Default $0 allows free ($0) pickups (Sleeper default).</summary>
    public decimal MinFaabBid { get; set; } = 0m;
}
