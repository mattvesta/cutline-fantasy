namespace Cutline.Core.Entities;

public class RosterSlot
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public Guid? PlayerId { get; set; }
    public Player? Player { get; set; }
    public SlotType SlotType { get; set; }
    public bool IsStarter { get; set; }
}

public enum SlotType { QB, RB, WR, TE, Flex, SuperFlex, K, DEF, Bench, IR }
