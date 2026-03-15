namespace Cutline.Ingest.Workers;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Ingest.Sports;

/// <summary>
/// Runs once per week. Fetches cross-platform player ID mappings from nflverse load_rosters()
/// and reconciles gsis_id → sleeper_id / espn_id / etc. on all Player records.
/// This is the bridge that lets the ESPN live scoring feed map back to Cutline's player records.
/// Uses IServiceScopeFactory because DbContext is scoped.
/// </summary>
public class NflverseRosterSyncWorker : BackgroundService
{
    private static readonly TimeSpan Period = TimeSpan.FromDays(7);

    private readonly NflverseClient _nflverse;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NflverseRosterSyncWorker> _logger;

    public NflverseRosterSyncWorker(
        NflverseClient nflverse,
        IServiceScopeFactory scopeFactory,
        ILogger<NflverseRosterSyncWorker> logger)
    {
        _nflverse = nflverse;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(Period);
        do
        {
            try
            {
                var season = CurrentNflSeason();
                _logger.LogInformation("Starting nflverse roster ID mapping sync for {Season}", season);

                var mappings = await _nflverse.FetchRosterIdMappingsAsync(season, ct);
                _logger.LogInformation("Fetched {Count} ID mappings from nflverse", mappings.Count);

                var patches = mappings
                    .Select(m => new PlayerIdPatch(m.GsisId, m.EspnId, m.SleeperId, m.YahooId))
                    .ToList();

                await using var scope = _scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();
                await repo.PatchIdsAsync(patches, ct);

                _logger.LogInformation("nflverse roster sync complete — {Count} players patched", patches.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "nflverse roster sync failed");
            }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }

    /// <summary>
    /// The NFL season year. The new season begins in September, so January–August
    /// still belong to the prior year's season.
    /// </summary>
    private static int CurrentNflSeason()
    {
        var now = DateTimeOffset.UtcNow;
        return now.Month >= 9 ? now.Year : now.Year - 1;
    }
}
