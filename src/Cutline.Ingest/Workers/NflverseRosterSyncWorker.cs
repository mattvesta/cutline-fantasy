namespace Cutline.Ingest.Workers;

using Cutline.Core.Interfaces;
using Cutline.Ingest.Sports;

/// <summary>
/// Runs once per week. Fetches cross-platform player ID mappings from nflverse load_rosters()
/// and reconciles gsis_id → sleeper_id / espn_id / etc. on all Player records.
/// This is the bridge that lets the ESPN live scoring feed map back to Cutline's player records.
/// </summary>
public class NflverseRosterSyncWorker : BackgroundService
{
    private static readonly TimeSpan Period = TimeSpan.FromDays(7);

    private readonly NflverseClient _nflverse;
    private readonly IPlayerRepository _players;
    private readonly ILogger<NflverseRosterSyncWorker> _logger;

    public NflverseRosterSyncWorker(NflverseClient nflverse, IPlayerRepository players, ILogger<NflverseRosterSyncWorker> logger)
    {
        _nflverse = nflverse;
        _players = players;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(Period);
        do
        {
            try
            {
                _logger.LogInformation("Starting nflverse roster ID mapping sync");
                // TODO: resolve current season
                var mappings = await _nflverse.FetchRosterIdMappingsAsync(season: 2025, ct);
                _logger.LogInformation("nflverse roster sync complete — {Count} ID mappings", mappings.Count);
                // TODO: apply mappings to player records
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "nflverse roster sync failed");
            }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }
}
