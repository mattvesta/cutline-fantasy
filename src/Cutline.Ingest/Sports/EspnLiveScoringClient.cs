namespace Cutline.Ingest.Sports;

/// <summary>
/// ESPN unofficial API client for live in-game scoring.
/// Used on game day only — driven by EspnLiveScoringWorker.
/// Maps espn_id back to gsis_id via nflverse ID map.
/// Once a game completes, final scores are reconciled against nflverse load_player_stats().
/// WARNING: Undocumented API with no SLA — handle failures gracefully.
/// </summary>
public class EspnLiveScoringClient
{
    private readonly HttpClient _http;

    public EspnLiveScoringClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<LivePlayerScore>> FetchLiveScoresAsync(int week, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}

public record LivePlayerScore(string EspnId, decimal Points, bool IsGameComplete);
