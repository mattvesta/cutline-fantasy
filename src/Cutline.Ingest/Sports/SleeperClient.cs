namespace Cutline.Ingest.Sports;

using System.Text.Json;
using System.Text.Json.Serialization;
using Cutline.Core.Entities;

/// <summary>
/// Sleeper API client. Fetches player metadata, injury status, depth charts.
/// Data is ingested in bulk — never per-league.
/// Sync cadence: daily (driven by SleeperSyncWorker).
/// </summary>
public class SleeperClient
{
    private const string PlayersUrl = "https://api.sleeper.app/v1/players/nfl";

    private static readonly HashSet<string> RelevantPositions =
        new(StringComparer.OrdinalIgnoreCase) { "QB", "RB", "WR", "TE", "K", "DEF", "FB" };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;

    public SleeperClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<Player>> FetchAllPlayersAsync(CancellationToken ct = default)
    {
        var response = await _http.GetStreamAsync(PlayersUrl, ct);
        var raw = await JsonSerializer.DeserializeAsync<Dictionary<string, SleeperPlayerDto>>(
            response, JsonOptions, ct);

        if (raw is null) return [];

        var now = DateTimeOffset.UtcNow;
        var players = new List<Player>();

        foreach (var (sleeperId, dto) in raw)
        {
            if (!RelevantPositions.Contains(dto.Position ?? string.Empty)) continue;

            var gsisId = string.IsNullOrWhiteSpace(dto.GsisId) ? null : dto.GsisId.Trim();

            players.Add(new Player
            {
                GsisId = gsisId,
                SleeperId = sleeperId,
                EspnId = dto.EspnId,
                FirstName = dto.FirstName ?? string.Empty,
                LastName = dto.LastName ?? string.Empty,
                Position = dto.Position ?? string.Empty,
                NflTeam = dto.Team,
                Status = MapStatus(dto.Status, dto.InjuryStatus),
                Age = dto.Age,
                College = dto.College,
                Height = dto.Height,
                Weight = dto.Weight,
                JerseyNumber = dto.Number,
                YearsExperience = dto.YearsExp,
                DepthChartOrder = dto.DepthChartOrder,
                // search_rank >= 9999 means unranked in Sleeper
                Adp = dto.SearchRank is > 0 and < 9999 ? (decimal)dto.SearchRank.Value : null,
                LastSyncedAt = now,
            });
        }

        return players;
    }

    private static PlayerStatus MapStatus(string? status, string? injuryStatus) =>
        status switch
        {
            "Injured Reserve" => PlayerStatus.InjuredReserve,
            "Active" when string.IsNullOrEmpty(injuryStatus) => PlayerStatus.Active,
            "Active" => PlayerStatus.Injured,
            "Inactive" => PlayerStatus.Inactive,
            _ => PlayerStatus.Unknown,
        };
}

file record SleeperPlayerDto
{
    [JsonPropertyName("first_name")] public string? FirstName { get; init; }
    [JsonPropertyName("last_name")] public string? LastName { get; init; }
    [JsonPropertyName("position")] public string? Position { get; init; }
    [JsonPropertyName("team")] public string? Team { get; init; }
    [JsonPropertyName("status")] public string? Status { get; init; }
    [JsonPropertyName("injury_status")] public string? InjuryStatus { get; init; }
    [JsonPropertyName("gsis_id")] public string? GsisId { get; init; }

    [JsonPropertyName("espn_id")]
    [JsonConverter(typeof(IntOrNullToStringConverter))]
    public string? EspnId { get; init; }

    [JsonPropertyName("age")] public int? Age { get; init; }
    [JsonPropertyName("college")] public string? College { get; init; }
    [JsonPropertyName("height")] public string? Height { get; init; }
    [JsonPropertyName("weight")] public string? Weight { get; init; }
    [JsonPropertyName("number")] public int? Number { get; init; }
    [JsonPropertyName("years_exp")] public int? YearsExp { get; init; }
    [JsonPropertyName("depth_chart_order")] public int? DepthChartOrder { get; init; }
    [JsonPropertyName("search_rank")] public double? SearchRank { get; init; }
}

/// <summary>Sleeper returns espn_id as a JSON integer, not a string.</summary>
file class IntOrNullToStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt64().ToString(),
            JsonTokenType.String => reader.GetString(),
            _ => null,
        };

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
