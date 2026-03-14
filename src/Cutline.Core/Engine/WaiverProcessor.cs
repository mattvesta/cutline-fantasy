namespace Cutline.Core.Engine;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;

/// <summary>
/// Processes waiver claims in priority order after elimination.
/// Supports both priority-based and FAAB waiver systems.
/// </summary>
public class WaiverProcessor
{
    private readonly IWeekRepository _weeks;
    private readonly ITeamRepository _teams;

    public WaiverProcessor(IWeekRepository weeks, ITeamRepository teams)
    {
        _weeks = weeks;
        _teams = teams;
    }

    public async Task<IReadOnlyList<WaiverClaim>> ProcessClaimsAsync(
        Guid leagueId,
        int weekNumber,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
