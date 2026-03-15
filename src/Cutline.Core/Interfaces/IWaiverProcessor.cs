namespace Cutline.Core.Interfaces;

using Cutline.Core.Entities;

public interface IWaiverProcessor
{
    /// <summary>
    /// Processes all pending waiver claims for the week in priority order (or FAAB
    /// bid order if the league uses FAAB). Sets week status to Completed when done.
    /// </summary>
    Task<WaiverProcessingResult> ProcessClaimsAsync(Guid leagueId, int weekNumber, CancellationToken ct = default);
}
