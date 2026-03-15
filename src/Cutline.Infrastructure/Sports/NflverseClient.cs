namespace Cutline.Infrastructure.Sports;

using System.Text;
using Cutline.Core.Entities;

/// <summary>
/// nflverse data client.
/// - load_rosters(): cross-platform player ID mapping (gsis_id, sleeper_id, espn_id, etc.)
/// - load_player_stats(): final weekly stats, authoritative source for scoring
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
        var gsisIdx      = IndexOf(headers, "gsis_id");
        var espnIdx      = IndexOf(headers, "espn_id");
        var sleeperIdx   = IndexOf(headers, "sleeper_id");
        var yahooIdx     = IndexOf(headers, "yahoo_id");
        var firstNameIdx = IndexOf(headers, "first_name");
        var lastNameIdx  = IndexOf(headers, "last_name");
        var positionIdx  = IndexOf(headers, "position");
        var teamIdx      = IndexOf(headers, "team");

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
                MflId:     null,
                FirstName: Field(fields, firstNameIdx),
                LastName:  Field(fields, lastNameIdx),
                Position:  Field(fields, positionIdx),
                Team:      Field(fields, teamIdx));
        }

        return byGsis.Values.ToList();
    }

    /// <summary>Fetches stats for a single week of a season (post-game reconciliation).</summary>
    public async Task<IReadOnlyList<PlayerWeekStats>> FetchFinalStatsAsync(int season, int week, CancellationToken ct = default)
        => await FetchPlayerStatsCsvAsync(season, week, ct);

    /// <summary>Fetches all regular-season weeks for a full season (historical backfill).</summary>
    public async Task<IReadOnlyList<PlayerWeekStats>> FetchAllSeasonStatsAsync(int season, CancellationToken ct = default)
        => await FetchPlayerStatsCsvAsync(season, week: null, ct);

    private async Task<IReadOnlyList<PlayerWeekStats>> FetchPlayerStatsCsvAsync(int season, int? week, CancellationToken ct)
    {
        // nflverse reorganised their releases starting with 2025: the old
        // player_stats release only goes up to 2024. Try the legacy URL first;
        // on 404 fall back to the new stats_player release layout.
        var primaryUrl  = $"https://github.com/nflverse/nflverse-data/releases/download/player_stats/player_stats_{season}.csv";
        var fallbackUrl = $"https://github.com/nflverse/nflverse-data/releases/download/stats_player/stats_player_week_{season}.csv";

        var primaryResponse = await _http.GetAsync(primaryUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        HttpResponseMessage response;
        if (primaryResponse.IsSuccessStatusCode)
        {
            response = primaryResponse;
        }
        else
        {
            primaryResponse.Dispose();
            response = await _http.GetAsync(fallbackUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
        }

        using var _ = response;

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        var headerLine = await reader.ReadLineAsync(ct);
        if (headerLine is null) return [];

        var h = ParseCsvLine(headerLine);

        var playerIdIdx      = IndexOf(h, "player_id");
        var seasonIdx        = IndexOf(h, "season");
        var weekIdx          = IndexOf(h, "week");
        var seasonTypeIdx    = IndexOf(h, "season_type");
        var completionsIdx   = IndexOf(h, "completions");
        var attemptsIdx      = IndexOf(h, "attempts");
        var passingYardsIdx  = IndexOf(h, "passing_yards");
        var passingTdsIdx    = IndexOf(h, "passing_tds");
        // Column renamed "interceptions" → "passing_interceptions" in the 2025 release layout
        var intIdx           = IndexOf(h, "interceptions");
        if (intIdx < 0) intIdx = IndexOf(h, "passing_interceptions");
        var carriesIdx       = IndexOf(h, "carries");
        var rushYardsIdx     = IndexOf(h, "rushing_yards");
        var rushTdsIdx       = IndexOf(h, "rushing_tds");
        var receptionsIdx    = IndexOf(h, "receptions");
        var targetsIdx       = IndexOf(h, "targets");
        var recYardsIdx      = IndexOf(h, "receiving_yards");
        var recTdsIdx        = IndexOf(h, "receiving_tds");
        var sackFumIdx       = IndexOf(h, "sack_fumbles_lost");
        var rushFumIdx       = IndexOf(h, "rushing_fumbles_lost");
        var recFumIdx        = IndexOf(h, "receiving_fumbles_lost");

        if (playerIdIdx < 0 || weekIdx < 0) return [];

        var results = new List<PlayerWeekStats>();

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var f = ParseCsvLine(line);
            if (f.Length <= playerIdIdx) continue;

            var gsisId = Field(f, playerIdIdx);
            if (string.IsNullOrEmpty(gsisId)) continue;

            var seasonType = Field(f, seasonTypeIdx);
            if (!string.Equals(seasonType, "REG", StringComparison.OrdinalIgnoreCase)) continue;

            var rowWeek = ParseInt(Field(f, weekIdx));
            if (rowWeek is null) continue;
            if (week.HasValue && rowWeek != week.Value) continue;

            var rowSeason = ParseInt(Field(f, seasonIdx)) ?? season;

            results.Add(new PlayerWeekStats(
                GsisId:             gsisId,
                Season:             rowSeason,
                Week:               rowWeek.Value,
                PassingYards:       ParseDecimal(Field(f, passingYardsIdx)) ?? 0m,
                PassingTDs:         ParseInt(Field(f, passingTdsIdx)) ?? 0,
                PassingAttempts:    ParseInt(Field(f, attemptsIdx)) ?? 0,
                PassingCompletions: ParseInt(Field(f, completionsIdx)) ?? 0,
                RushingYards:       ParseDecimal(Field(f, rushYardsIdx)) ?? 0m,
                RushingTDs:         ParseInt(Field(f, rushTdsIdx)) ?? 0,
                RushingAttempts:    ParseInt(Field(f, carriesIdx)) ?? 0,
                ReceivingYards:     ParseDecimal(Field(f, recYardsIdx)) ?? 0m,
                ReceivingTDs:       ParseInt(Field(f, recTdsIdx)) ?? 0,
                Receptions:         ParseInt(Field(f, receptionsIdx)) ?? 0,
                Targets:            ParseInt(Field(f, targetsIdx)) ?? 0,
                Interceptions:      ParseInt(Field(f, intIdx)) ?? 0,
                FumblesLost:        (ParseInt(Field(f, sackFumIdx)) ?? 0)
                                  + (ParseInt(Field(f, rushFumIdx)) ?? 0)
                                  + (ParseInt(Field(f, recFumIdx)) ?? 0)
            ));
        }

        return results;
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

    private static int? ParseInt(string? s) =>
        int.TryParse(s, out var v) ? v : null;

    private static decimal? ParseDecimal(string? s) =>
        decimal.TryParse(s, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;

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
                    i++;
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

public record PlayerWeekStats(
    string  GsisId,
    int     Season,
    int     Week,
    decimal PassingYards,
    int     PassingTDs,
    int     PassingAttempts,
    int     PassingCompletions,
    decimal RushingYards,
    int     RushingTDs,
    int     RushingAttempts,
    decimal ReceivingYards,
    int     ReceivingTDs,
    int     Receptions,
    int     Targets,
    int     Interceptions,
    int     FumblesLost);
