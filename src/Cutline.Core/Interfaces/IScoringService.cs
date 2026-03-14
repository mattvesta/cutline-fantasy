namespace Cutline.Core.Interfaces;

using Cutline.Core.Entities;

public interface IScoringService
{
    /// <summary>
    /// Applies a league's scoring rules to bulk raw stats, returning per-team scores.
    /// Raw stats are ingested once and fanned out to all leagues.
    /// </summary>
    Task<IReadOnlyList<TeamScore>> CalculateScoresAsync(
        Guid leagueId,
        int weekNumber,
        CancellationToken ct = default);
}
