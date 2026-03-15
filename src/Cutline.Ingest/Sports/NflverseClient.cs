namespace Cutline.Ingest.Sports;

using System.Text;

/// <summary>
/// nflverse data client.
/// - load_rosters(): cross-platform player ID mapping (gsis_id, sleeper_id, espn_id, etc.) — weekly sync
/// - load_player_stats(): final weekly stats, authoritative source for scoring — post-game sync
/// gsis_id is the canonical player identifier throughout the system.
///
/// Data is served as CSV from GitHub release assets, which redirect to S3.
/// URL pattern: https://github.com/nflverse/nflverse-data/releases/download/rosters/roster_{season}.csv
/// </summary>
public class NflverseClient
{
    private readonly HttpClient _http;

    public NflverseClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<PlayerIdMap>> FetchRosterIdMappingsAsync(int season, CancellationToken ct = default)
    {
        var url = $"https://github.com/nflverse/nflverse-data/releases/download/rosters/roster_{season}.csv";

        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        var headerLine = await reader.ReadLineAsync(ct);
        if (headerLine is null) return [];

        var headers = ParseCsvLine(headerLine);
        var gsisIdx    = IndexOf(headers, "gsis_id");
        var espnIdx    = IndexOf(headers, "espn_id");
        var sleeperIdx = IndexOf(headers, "sleeper_id");
        var yahooIdx   = IndexOf(headers, "yahoo_id");

        if (gsisIdx < 0) return [];

        // Rows are ordered by week ascending — later rows overwrite, so the most
        // recent week's IDs win for each player.
        var byGsis = new Dictionary<string, PlayerIdMap>(StringComparer.Ordinal);

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fields = ParseCsvLine(line);
            if (fields.Length <= gsisIdx) continue;

            var gsisId = fields[gsisIdx].Trim();
            if (string.IsNullOrEmpty(gsisId)) continue;

            byGsis[gsisId] = new PlayerIdMap(
                GsisId:    gsisId,
                SleeperId: Field(fields, sleeperIdx),
                EspnId:    Field(fields, espnIdx),
                YahooId:   Field(fields, yahooIdx),
                MflId:     null);
        }

        return byGsis.Values.ToList();
    }

    public async Task<IReadOnlyList<PlayerWeekStats>> FetchFinalStatsAsync(int season, int week, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    // --- CSV helpers ---

    private static int IndexOf(string[] headers, string name)
    {
        for (var i = 0; i < headers.Length; i++)
            if (string.Equals(headers[i].Trim(), name, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    private static string? Field(string[] fields, int idx) =>
        idx >= 0 && idx < fields.Length ? NullIfEmpty(fields[idx]) : null;

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>Parses a single CSV line, respecting RFC 4180 double-quote escaping.</summary>
    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++; // skip escaped quote
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == '"')
                    inQuotes = true;
                else if (c == ',')
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else
                    sb.Append(c);
            }
        }

        fields.Add(sb.ToString());
        return fields.ToArray();
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
