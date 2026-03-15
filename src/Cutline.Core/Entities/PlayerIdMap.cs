namespace Cutline.Core.Entities;

/// <summary>
/// Cross-platform player ID mapping sourced from nflverse load_rosters().
/// Also carries name/position/team for GsisId backfill matching.
/// </summary>
public record PlayerIdMap(
    string  GsisId,
    string? SleeperId,
    string? EspnId,
    string? YahooId,
    string? MflId,
    string? FirstName,
    string? LastName,
    string? Position,
    string? Team);
