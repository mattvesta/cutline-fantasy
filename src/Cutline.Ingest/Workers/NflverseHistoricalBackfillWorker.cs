namespace Cutline.Ingest.Workers;

using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Sports;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Runs once at startup. For each past NFL season, fetches nflverse roster + stats CSVs,
/// resolves all GsisIds, and upserts PlayerGameStats rows.
///
/// Pipeline per season:
///   1. Fetch roster CSV → BackfillGsisIdsAsync (fills in GsisId on existing Sleeper players)
///   2. EnsureNflversePlayersAsync (creates Player rows for any GsisId not yet in the DB)
///   3. Fetch stats CSV → ImportAsync
///
/// Steps 1+2 guarantee that every player who appears in nflverse stats has a matching
/// Player row, so the skip count should be near zero.
/// </summary>
public class NflverseHistoricalBackfillWorker(
    NflverseClient nflverse,
    IServiceScopeFactory scopeFactory,
    ILogger<NflverseHistoricalBackfillWorker> logger) : BackgroundService
{
    private const int HistoricalStartSeason = 2020;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var currentSeason = CurrentNflSeason();

        for (var season = HistoricalStartSeason; season <= currentSeason; season++)
        {
            if (ct.IsCancellationRequested) break;

            logger.LogInformation("Historical backfill: starting season {Season}", season);
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();

                // Step 1 — fetch roster and resolve GsisIds for existing Sleeper players
                var roster = await nflverse.FetchRosterIdMappingsAsync(season, ct);
                logger.LogInformation("Historical backfill: {Season} — {Count} roster entries fetched", season, roster.Count);

                var backfilled = await repo.BackfillGsisIdsAsync(roster, ct);
                if (backfilled > 0)
                    logger.LogInformation("Historical backfill: {Season} — GsisId backfilled for {Count} players", season, backfilled);

                // Step 2 — create Player rows for any nflverse GsisId not yet in the DB
                var created = await repo.EnsureNflversePlayersAsync(roster, ct);
                if (created > 0)
                    logger.LogInformation("Historical backfill: {Season} — {Count} new players created from nflverse roster", season, created);

                // Step 3 — import stats
                var stats = await nflverse.FetchAllSeasonStatsAsync(season, ct);
                logger.LogInformation("Historical backfill: {Season} — {Count} stat rows fetched", season, stats.Count);

                var importer = scope.ServiceProvider.GetRequiredService<NflverseStatsImporter>();
                var (inserted, updated) = await importer.ImportAsync(stats, ct);

                logger.LogInformation(
                    "Historical backfill: season {Season} complete — {Inserted} inserted, {Updated} updated ({Skipped} skipped, no GsisId)",
                    season, inserted, updated, stats.Count - inserted - updated);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Historical backfill failed for season {Season}", season);
            }
        }

        logger.LogInformation("Historical backfill worker finished");
    }

    private static int CurrentNflSeason()
    {
        var now = DateTimeOffset.UtcNow;
        return now.Month >= 9 ? now.Year : now.Year - 1;
    }
}
