namespace Cutline.Ingest.Workers;

using Cutline.Ingest.Sports;

/// <summary>
/// Runs post-game (Sunday night / Monday). Fetches final weekly stats from nflverse
/// load_player_stats() and reconciles any live scores that were written during game day.
/// nflverse is the authoritative source of truth for final scoring — never use ESPN data
/// for locked scores.
/// </summary>
public class NflverseFinalStatsWorker : BackgroundService
{
    private readonly NflverseClient _nflverse;
    private readonly ILogger<NflverseFinalStatsWorker> _logger;

    public NflverseFinalStatsWorker(NflverseClient nflverse, ILogger<NflverseFinalStatsWorker> logger)
    {
        _nflverse = nflverse;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // TODO: trigger on a schedule aligned to post-game window (e.g. Monday 2am)
        // TODO: fetch final stats, fan out per-league scoring, lock TeamScore records,
        //       write final scores to Redis, trigger GuillotineEngine if all games complete
        await Task.CompletedTask;
    }
}
