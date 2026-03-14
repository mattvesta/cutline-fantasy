namespace Cutline.Ingest.Workers;

using Cutline.Infrastructure.Cache;
using Cutline.Ingest.Sports;
using StackExchange.Redis;

/// <summary>
/// Game day only. Polls the ESPN unofficial API on a configurable interval (default 60s)
/// during active game windows. Writes per-player live scores to Redis keyed by gsis_id.
/// The API reads from Redis and pushes updates to clients via SignalR — it never touches
/// this worker or the ESPN feed directly.
///
/// Stops polling once all games are complete; NflverseFinalStatsWorker takes over for
/// official score reconciliation.
/// </summary>
public class EspnLiveScoringWorker : BackgroundService
{
    private readonly EspnLiveScoringClient _espn;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<EspnLiveScoringWorker> _logger;
    private readonly TimeSpan _pollInterval;

    public EspnLiveScoringWorker(
        EspnLiveScoringClient espn,
        IConnectionMultiplexer redis,
        ILogger<EspnLiveScoringWorker> logger,
        IConfiguration config)
    {
        _espn = espn;
        _redis = redis;
        _logger = logger;
        _pollInterval = TimeSpan.FromSeconds(config.GetValue("Ingest:EspnPollIntervalSeconds", 60));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_pollInterval);
        do
        {
            try
            {
                // TODO: gate on active game window before polling
                // TODO: resolve current NFL week
                var scores = await _espn.FetchLiveScoresAsync(week: 1, ct);
                var db = _redis.GetDatabase();

                foreach (var score in scores)
                {
                    // TODO: map espn_id → gsis_id via cached nflverse ID map
                    // then write to Redis: stats:live:week:{week}
                    _ = db; // suppress unused warning until implemented
                }

                var complete = scores.Count > 0 && scores.All(s => s.IsGameComplete);
                if (complete)
                {
                    _logger.LogInformation("All games complete — stopping live scoring poll for this week");
                    break;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "ESPN live scoring poll failed — will retry next interval");
            }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }
}
