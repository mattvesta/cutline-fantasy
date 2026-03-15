namespace Cutline.Core.Interfaces;

using Cutline.Core.Entities;

public interface IGuillotineEngine
{
    /// <summary>
    /// Identifies the lowest-scoring active team for the given week, marks them
    /// eliminated, releases their roster to free agency, and sets the week status
    /// to Eliminated so waiver claims can be submitted.
    /// </summary>
    Task<EliminationResult> RunEliminationAsync(Guid leagueId, int weekNumber, CancellationToken ct = default);
}
