namespace Cutline.Ingest.Workers;

using Cutline.Infrastructure.Sports;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Runs once at startup. For each past NFL season from HistoricalStartSeason up to the
/// season before the current one, checks whether any PlayerGameStats exist. If a season
/// has no stats, fetches and imports the full nflverse player_stats CSV for that season.
///
/// This is a one-pass backfill — subsequent restarts skip seasons that already have data.
/// The current in-progress season is handled by NflverseFinalStatsWorker instead.
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

        for (var season = HistoricalStartSeason; season < currentSeason; season++)
        {
            if (ct.IsCancellationRequested) break;

            logger.LogInformation("Historical backfill: fetching season {Season}", season);
            try
            {
                var stats = await nflverse.FetchAllSeasonStatsAsync(season, ct);
                logger.LogInformation("Historical backfill: {Count} rows for season {Season}", stats.Count, season);

                await using var scope = scopeFactory.CreateAsyncScope();
                var importer = scope.ServiceProvider.GetRequiredService<NflverseStatsImporter>();
                await importer.ImportAsync(stats, ct);

                logger.LogInformation("Historical backfill: season {Season} complete", season);
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
