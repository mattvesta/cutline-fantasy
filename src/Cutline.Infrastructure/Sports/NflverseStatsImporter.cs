namespace Cutline.Infrastructure.Sports;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Converts nflverse PlayerWeekStats into PlayerGameStats rows and bulk-upserts them.
/// Keyed on (PlayerId, Season, WeekNumber) — safe to re-run; existing rows are updated.
/// Fantasy points are stored using standard PPR scoring so the player profile can show
/// season totals before any league-specific scoring context is available.
/// </summary>
public class NflverseStatsImporter(
    CutlineDbContext db,
    ILiveScoringService scoring,
    ILogger<NflverseStatsImporter> logger)
{
    // PPR is the most common format; league-specific scoring is applied separately at
    // the per-team scoring layer, not here.
    private static readonly ScoringSettings PprScoring = new() { ReceptionPoints = 1m };

    public async Task ImportAsync(IReadOnlyList<PlayerWeekStats> weekStats, CancellationToken ct)
    {
        if (weekStats.Count == 0) return;

        // Build GsisId → Player.Id lookup for all incoming rows in one query.
        var gsisIds = weekStats.Select(s => s.GsisId).Distinct().ToHashSet();
        var playerIdLookup = await db.Players
            .Where(p => p.GsisId != null && gsisIds.Contains(p.GsisId!))
            .ToDictionaryAsync(p => p.GsisId!, p => p.Id, ct);

        var skipped = weekStats.Count(s => !playerIdLookup.ContainsKey(s.GsisId));
        if (skipped > 0)
            logger.LogDebug("{Count} rows skipped — GsisId not in Players table", skipped);

        // Process one week at a time so each SaveChanges is a manageable transaction.
        var byWeek = weekStats
            .Where(s => playerIdLookup.ContainsKey(s.GsisId))
            .GroupBy(s => (s.Season, s.Week));

        foreach (var group in byWeek)
        {
            var (season, week) = group.Key;
            var rows = group.ToList();

            var playerIds = rows.Select(s => playerIdLookup[s.GsisId]).ToList();
            var existing = await db.PlayerGameStats
                .Where(s => s.Season == season && s.WeekNumber == week && playerIds.Contains(s.PlayerId))
                .ToDictionaryAsync(s => s.PlayerId, ct);

            foreach (var ws in rows)
            {
                var playerId = playerIdLookup[ws.GsisId];

                if (existing.TryGetValue(playerId, out var record))
                {
                    Apply(ws, record);
                }
                else
                {
                    record = new PlayerGameStats
                    {
                        Id         = Guid.NewGuid(),
                        PlayerId   = playerId,
                        Season     = season,
                        WeekNumber = week,
                    };
                    Apply(ws, record);
                    db.PlayerGameStats.Add(record);
                }
            }

            await db.SaveChangesAsync(ct);
        }
    }

    private void Apply(PlayerWeekStats ws, PlayerGameStats gs)
    {
        gs.PassingYards        = (int)ws.PassingYards;
        gs.PassingTDs          = ws.PassingTDs;
        gs.PassingAttempts     = ws.PassingAttempts;
        gs.PassingCompletions  = ws.PassingCompletions;
        gs.Interceptions       = ws.Interceptions;
        gs.RushingYards        = (int)ws.RushingYards;
        gs.RushingTDs          = ws.RushingTDs;
        gs.RushingAttempts     = ws.RushingAttempts;
        gs.ReceivingYards      = (int)ws.ReceivingYards;
        gs.ReceivingTDs        = ws.ReceivingTDs;
        gs.Receptions          = ws.Receptions;
        gs.Targets             = ws.Targets;
        gs.Fumbles             = ws.FumblesLost;
        gs.GameStatus          = GameStatus.Final;
        gs.Points              = scoring.CalculatePoints(gs, PprScoring);
        gs.LastUpdated         = DateTime.UtcNow;
    }
}
