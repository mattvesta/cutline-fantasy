namespace Cutline.Ingest.Sports;

using Cutline.Core.Entities;

/// <summary>
/// Sleeper API client. Fetches player metadata, injury status, bye weeks, depth charts.
/// Data is ingested in bulk — never per-league.
/// Sync cadence: daily (driven by SleeperSyncWorker).
/// </summary>
public class SleeperClient
{
    private readonly HttpClient _http;

    public SleeperClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<Player>> FetchAllPlayersAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
