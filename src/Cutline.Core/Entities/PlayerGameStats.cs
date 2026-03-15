namespace Cutline.Core.Entities;

public class PlayerGameStats
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public int Season { get; set; }
    public int WeekNumber { get; set; }

    /// <summary>Pre-calculated fantasy points using the league's ScoringSettings.</summary>
    public decimal Points { get; set; }

    public GameStatus GameStatus { get; set; } = GameStatus.Scheduled;

    /// <summary>Short string e.g. "vs KC" or "@ DAL"</summary>
    public string? Opponent { get; set; }

    // ── Passing ──────────────────────────────────────────────────────────
    public int? PassingYards { get; set; }
    public int? PassingTDs { get; set; }
    public int? Interceptions { get; set; }
    public int? PassingAttempts { get; set; }
    public int? PassingCompletions { get; set; }

    // ── Rushing ──────────────────────────────────────────────────────────
    public int? RushingYards { get; set; }
    public int? RushingTDs { get; set; }
    public int? RushingAttempts { get; set; }
    public int? Fumbles { get; set; }

    // ── Receiving ─────────────────────────────────────────────────────────
    public int? Receptions { get; set; }
    public int? Targets { get; set; }
    public int? ReceivingYards { get; set; }
    public int? ReceivingTDs { get; set; }

    // ── Kicker ────────────────────────────────────────────────────────────
    public int? FieldGoalsMade { get; set; }
    public int? FieldGoalsAttempted { get; set; }
    public int? LongFieldGoal { get; set; }
    public int? ExtraPointsMade { get; set; }
    public int? ExtraPointsAttempted { get; set; }

    // ── Defense / Special Teams ───────────────────────────────────────────
    public int? Sacks { get; set; }
    public int? DefensiveInterceptions { get; set; }
    public int? FumblesRecovered { get; set; }
    public int? DefensiveTDs { get; set; }
    public int? PointsAllowed { get; set; }
    public int? Safeties { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public enum GameStatus { Scheduled, InProgress, Final, Bye }
