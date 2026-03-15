namespace Cutline.Ingest.Workers;

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
        logger.LogInformation("NflverseFinalStatsWorker: fetching season {Season} stats", season);

        try
        {
            var stats = await nflverse.FetchAllSeasonStatsAsync(season, ct);
            logger.LogInformation("Fetched {Count} stat rows for season {Season}", stats.Count, season);

            await using var scope = scopeFactory.CreateAsyncScope();
            var importer = scope.ServiceProvider.GetRequiredService<NflverseStatsImporter>();
            await importer.ImportAsync(stats, ct);

            logger.LogInformation("NflverseFinalStatsWorker: season {Season} import complete", season);
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
