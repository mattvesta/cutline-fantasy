namespace Cutline.Ingest.Workers;

using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Sports;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Runs every Monday at ~8am UTC — after nflverse publishes final player_stats for the
/// completed NFL week. Re-fetches the full current-season CSV and upserts all weeks,
/// so missed runs self-correct on the next execution.
/// nflverse is the authoritative source; ESPN live data is overwritten here.
/// </summary>
public class NflverseFinalStatsWorker(
    NflverseClient nflverse,
    IServiceScopeFactory scopeFactory,
    ILogger<NflverseFinalStatsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Wait until the first Monday 8am UTC window, then repeat weekly.
        await WaitUntilNextMondayAsync(ct);

        using var timer = new PeriodicTimer(TimeSpan.FromDays(7));
        do
        {
            await RunAsync(ct);
        }
        while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task RunAsync(CancellationToken ct)
    {
        var season = CurrentNflSeason();
        logger.LogInformation("NflverseFinalStatsWorker: starting season {Season}", season);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();

            // Resolve GsisIds on existing players, then create any still-missing ones
            var roster = await nflverse.FetchRosterIdMappingsAsync(season, ct);

            var backfilled = await repo.BackfillGsisIdsAsync(roster, ct);
            if (backfilled > 0)
                logger.LogInformation("NflverseFinalStatsWorker: GsisId backfilled for {Count} players", backfilled);

            var created = await repo.EnsureNflversePlayersAsync(roster, ct);
            if (created > 0)
                logger.LogInformation("NflverseFinalStatsWorker: {Count} new players created from nflverse roster", created);

            var stats = await nflverse.FetchAllSeasonStatsAsync(season, ct);
            logger.LogInformation("NflverseFinalStatsWorker: fetched {Count} stat rows for season {Season}", stats.Count, season);

            var importer = scope.ServiceProvider.GetRequiredService<NflverseStatsImporter>();
            var (inserted, updated) = await importer.ImportAsync(stats, ct);

            logger.LogInformation(
                "NflverseFinalStatsWorker: season {Season} complete — {Inserted} inserted, {Updated} updated ({Skipped} skipped, no GsisId)",
                season, inserted, updated, stats.Count - inserted - updated);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "NflverseFinalStatsWorker failed for season {Season}", season);
        }
    }

    /// <summary>Sleeps until the next Monday 8:00 UTC.</summary>
    private static async Task WaitUntilNextMondayAsync(CancellationToken ct)
    {
        var now  = DateTime.UtcNow;
        var next = now.DayOfWeek == DayOfWeek.Monday && now.Hour < 8
            ? now.Date.AddHours(8)
            : NextWeekday(now, DayOfWeek.Monday).AddHours(8);

        var delay = next - now;
        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, ct);
    }

    private static DateTime NextWeekday(DateTime from, DayOfWeek day)
    {
        var daysUntil = ((int)day - (int)from.DayOfWeek + 7) % 7;
        if (daysUntil == 0) daysUntil = 7;
        return from.Date.AddDays(daysUntil);
    }

    private static int CurrentNflSeason()
    {
        var now = DateTimeOffset.UtcNow;
        return now.Month >= 9 ? now.Year : now.Year - 1;
    }
}
