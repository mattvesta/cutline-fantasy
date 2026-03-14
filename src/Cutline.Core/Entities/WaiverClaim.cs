namespace Cutline.Core.Entities;

public class WaiverClaim
{
    public Guid Id { get; set; }
    public Guid WeekId { get; set; }
    public Week Week { get; set; } = null!;
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public Guid AddPlayerId { get; set; }
    public Player AddPlayer { get; set; } = null!;
    public Guid? DropPlayerId { get; set; }
    public Player? DropPlayer { get; set; }
    public int Priority { get; set; }
    public decimal? FaabBid { get; set; }
    public WaiverClaimStatus Status { get; set; }
    public string? RejectionReason { get; set; }
}

public enum WaiverClaimStatus { Pending, Processed, Rejected }
