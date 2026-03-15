namespace Cutline.Core.Entities;

public record EliminationResult(
    Guid EliminatedTeamId,
    string EliminatedTeamName,
    decimal LosingScore,
    int WeekNumber,
    int PlayersReleased);

public record WaiverProcessingResult(
    int ProcessedCount,
    int RejectedCount,
    IReadOnlyList<WaiverClaimOutcome> Claims);

public record WaiverClaimOutcome(
    Guid ClaimId,
    Guid TeamId,
    Guid AddPlayerId,
    Guid? DropPlayerId,
    WaiverClaimStatus Status,
    string? RejectionReason);
