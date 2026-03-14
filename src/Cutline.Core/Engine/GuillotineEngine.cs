namespace Cutline.Core.Engine;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;

/// <summary>
/// Core elimination engine. Identifies the lowest-scoring team each week,
/// triggers elimination, and releases their roster to waivers.
/// Elimination records are immutable — never mutate a completed Week.
/// </summary>
public class GuillotineEngine
{
    private readonly IWeekRepository _weeks;
    private readonly ITeamRepository _teams;
    private readonly IScoringService _scoring;

    public GuillotineEngine(
        IWeekRepository weeks,
        ITeamRepository teams,
        IScoringService scoring)
    {
        _weeks = weeks;
        _teams = teams;
        _scoring = scoring;
    }

    public async Task<EliminationResult> RunEliminationAsync(
        Guid leagueId,
        int weekNumber,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}

public record EliminationResult(
    Guid EliminatedTeamId,
    decimal LowestScore,
    IReadOnlyList<Guid> ReleasedPlayerIds);
