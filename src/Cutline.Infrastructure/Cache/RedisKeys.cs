namespace Cutline.Infrastructure.Cache;

/// <summary>
/// Centralised Redis key definitions. All league-scoped keys follow the convention: league:{leagueId}:*
/// </summary>
public static class RedisKeys
{
    // League-scoped
    public static string LeagueStandings(Guid leagueId) => $"league:{leagueId}:standings";
    public static string LeagueWeekScore(Guid leagueId, int week) => $"league:{leagueId}:week:{week}:scores";
    public static string TeamLiveScore(Guid leagueId, Guid teamId, int week) => $"league:{leagueId}:week:{week}:team:{teamId}:live";

    // Global (not league-scoped)
    public static string AllPlayers() => "players:all";
    public static string PlayerById(string gsisId) => $"players:{gsisId}";
    public static string BulkStats(int season, int week) => $"stats:{season}:week:{week}:raw";
    public static string LiveScores(int week) => $"stats:live:week:{week}";
}
