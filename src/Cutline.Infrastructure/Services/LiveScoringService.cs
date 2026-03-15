namespace Cutline.Infrastructure.Services;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class LiveScoringService(CutlineDbContext db) : ILiveScoringService
{
    public async Task<List<PlayerGameStats>> GetPlayerStatsAsync(
        int season, int weekNumber, IEnumerable<Guid> playerIds, CancellationToken ct = default)
    {
        var ids = playerIds.ToList();
        return await db.PlayerGameStats
            .Include(s => s.Player)
            .Where(s => s.Season == season && s.WeekNumber == weekNumber && ids.Contains(s.PlayerId))
            .ToListAsync(ct);
    }

    public async Task UpsertAsync(PlayerGameStats stats, CancellationToken ct = default)
    {
        var existing = await db.PlayerGameStats
            .FirstOrDefaultAsync(s =>
                s.PlayerId == stats.PlayerId &&
                s.Season == stats.Season &&
                s.WeekNumber == stats.WeekNumber, ct);

        if (existing is null)
        {
            stats.Id = Guid.NewGuid();
            db.PlayerGameStats.Add(stats);
        }
        else
        {
            existing.Points                  = stats.Points;
            existing.GameStatus              = stats.GameStatus;
            existing.Opponent                = stats.Opponent;
            existing.PassingYards            = stats.PassingYards;
            existing.PassingTDs              = stats.PassingTDs;
            existing.Interceptions           = stats.Interceptions;
            existing.PassingAttempts         = stats.PassingAttempts;
            existing.PassingCompletions      = stats.PassingCompletions;
            existing.RushingYards            = stats.RushingYards;
            existing.RushingTDs              = stats.RushingTDs;
            existing.RushingAttempts         = stats.RushingAttempts;
            existing.Fumbles                 = stats.Fumbles;
            existing.Receptions              = stats.Receptions;
            existing.Targets                 = stats.Targets;
            existing.ReceivingYards          = stats.ReceivingYards;
            existing.ReceivingTDs            = stats.ReceivingTDs;
            existing.FieldGoalsMade          = stats.FieldGoalsMade;
            existing.FieldGoalsAttempted     = stats.FieldGoalsAttempted;
            existing.LongFieldGoal           = stats.LongFieldGoal;
            existing.ExtraPointsMade         = stats.ExtraPointsMade;
            existing.ExtraPointsAttempted    = stats.ExtraPointsAttempted;
            existing.Sacks                   = stats.Sacks;
            existing.DefensiveInterceptions  = stats.DefensiveInterceptions;
            existing.FumblesRecovered        = stats.FumblesRecovered;
            existing.DefensiveTDs            = stats.DefensiveTDs;
            existing.PointsAllowed           = stats.PointsAllowed;
            existing.Safeties                = stats.Safeties;
            existing.LastUpdated             = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task UpsertRangeAsync(IEnumerable<PlayerGameStats> statsList, CancellationToken ct = default)
    {
        foreach (var stats in statsList)
            await UpsertAsync(stats, ct);
    }

    public decimal CalculatePoints(PlayerGameStats s, ScoringSettings scoring)
    {
        decimal pts = 0;

        // Passing
        pts += (s.PassingYards ?? 0) / scoring.PassingYardsPerPoint;
        pts += (s.PassingTDs ?? 0)   * scoring.PassingTdPoints;
        pts += (s.Interceptions ?? 0) * scoring.InterceptionPoints;

        // Rushing
        pts += (s.RushingYards ?? 0) / scoring.RushingYardsPerPoint;
        pts += (s.RushingTDs ?? 0)   * scoring.RushingTdPoints;

        // Receiving
        pts += (s.ReceivingYards ?? 0) / scoring.ReceivingYardsPerPoint;
        pts += (s.ReceivingTDs ?? 0)   * scoring.ReceivingTdPoints;
        pts += (s.Receptions ?? 0)     * scoring.ReceptionPoints;

        // Fumbles
        pts += (s.Fumbles ?? 0) * scoring.FumbleLostPoints;

        // Kicker (standard: 3/4/5 pts per FG by distance, 1 pt XP)
        if (s.FieldGoalsMade is > 0)
        {
            // Estimate: split evenly if no long FG info; otherwise grade up
            decimal fgPts = s.LongFieldGoal >= 50 ? 5m :
                            s.LongFieldGoal >= 40 ? 4m : 3m;
            pts += s.FieldGoalsMade.Value * fgPts;
        }
        if (s.FieldGoalsMade.HasValue && s.FieldGoalsAttempted.HasValue)
            pts -= (s.FieldGoalsAttempted.Value - s.FieldGoalsMade.Value) * 1m;
        pts += (s.ExtraPointsMade ?? 0) * 1m;

        // Defense / ST
        pts += (s.Sacks                  ?? 0) * 1m;
        pts += (s.DefensiveInterceptions ?? 0) * 2m;
        pts += (s.FumblesRecovered       ?? 0) * 2m;
        pts += (s.DefensiveTDs           ?? 0) * 6m;
        pts += (s.Safeties               ?? 0) * 2m;
        pts += s.PointsAllowed switch
        {
            null              =>  0m,
            0                 => 10m,
            <= 6              =>  7m,
            <= 13             =>  4m,
            <= 17             =>  1m,
            <= 27             =>  0m,
            <= 34             => -1m,
            _                 => -4m,
        };

        return Math.Round(pts, 2);
    }

    public async Task<decimal> GetTeamStarterPointsAsync(
        Guid teamId, int season, int weekNumber, CancellationToken ct = default)
    {
        var starterPlayerIds = await db.RosterSlots
            .Where(rs => rs.TeamId == teamId && rs.IsStarter && rs.PlayerId != null)
            .Select(rs => rs.PlayerId!.Value)
            .ToListAsync(ct);

        if (starterPlayerIds.Count == 0) return 0m;

        return await db.PlayerGameStats
            .Where(s =>
                s.Season == season &&
                s.WeekNumber == weekNumber &&
                starterPlayerIds.Contains(s.PlayerId))
            .SumAsync(s => s.Points, ct);
    }
}
