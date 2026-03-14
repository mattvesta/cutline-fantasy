namespace Cutline.Ingest.Workers;

using Cutline.Core.Interfaces;
using Cutline.Ingest.Sports;

/// <summary>
/// Runs once daily. Fetches all player metadata from Sleeper in bulk and upserts into the DB.
/// Bulk ingest — the API reads this data; it never calls Sleeper directly.
/// </summary>
public class SleeperSyncWorker : BackgroundService
{
    private static readonly TimeSpan Period = TimeSpan.FromHours(24);

    private readonly SleeperClient _sleeper;
    private readonly IPlayerRepository _players;
    private readonly ILogger<SleeperSyncWorker> _logger;

    public SleeperSyncWorker(SleeperClient sleeper, IPlayerRepository players, ILogger<SleeperSyncWorker> logger)
    {
        _sleeper = sleeper;
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
                _logger.LogInformation("Starting Sleeper player sync");
                var players = await _sleeper.FetchAllPlayersAsync(ct);
                await _players.UpsertBulkAsync(players, ct);
                _logger.LogInformation("Sleeper sync complete — {Count} players upserted", players.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Sleeper sync failed");
            }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }
}
