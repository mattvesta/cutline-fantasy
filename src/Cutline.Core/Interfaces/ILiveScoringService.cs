namespace Cutline.Core.Interfaces;

using Cutline.Core.Entities;

public interface ILiveScoringService
{
    /// <summary>Fetch stats for a specific set of player IDs for the given week.</summary>
    Task<List<PlayerGameStats>> GetPlayerStatsAsync(
        int season, int weekNumber, IEnumerable<Guid> playerIds, CancellationToken ct = default);

    /// <summary>Save or update a player's stats record.</summary>
    Task UpsertAsync(PlayerGameStats stats, CancellationToken ct = default);

    /// <summary>Save or update many stats records in one shot.</summary>
    Task UpsertRangeAsync(IEnumerable<PlayerGameStats> stats, CancellationToken ct = default);

    /// <summary>Calculate fantasy points for a stats record using the given settings.</summary>
    decimal CalculatePoints(PlayerGameStats stats, ScoringSettings scoring);

    /// <summary>Sum starter points for a team for a given week.</summary>
    Task<decimal> GetTeamStarterPointsAsync(
        Guid teamId, int season, int weekNumber, CancellationToken ct = default);
}
