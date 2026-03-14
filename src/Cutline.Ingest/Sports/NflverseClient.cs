namespace Cutline.Ingest.Sports;

/// <summary>
/// nflverse data client.
/// - load_rosters(): cross-platform player ID mapping (gsis_id, sleeper_id, espn_id, etc.) — weekly sync
/// - load_player_stats(): final weekly stats, authoritative source for scoring — post-game sync
/// gsis_id is the canonical player identifier throughout the system.
/// </summary>
public class NflverseClient
{
    private readonly HttpClient _http;

    public NflverseClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<PlayerIdMap>> FetchRosterIdMappingsAsync(int season, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<PlayerWeekStats>> FetchFinalStatsAsync(int season, int week, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}

public record PlayerIdMap(
    string GsisId,
    string? SleeperId,
    string? EspnId,
    string? YahooId,
    string? MflId);

public record PlayerWeekStats(
    string GsisId,
    int Week,
    decimal PassingYards,
    int PassingTds,
    decimal RushingYards,
    int RushingTds,
    decimal ReceivingYards,
    int ReceivingTds,
    int Receptions,
    int Interceptions,
    int FumblesLost);
