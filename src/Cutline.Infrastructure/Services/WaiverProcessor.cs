namespace Cutline.Infrastructure.Services;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Processes waiver claims after a team has been eliminated.
///
/// Ordering:
///   - Standard waivers: ascending Priority (lower number = higher priority)
///   - FAAB: descending FaabBid, ties broken by ascending Priority
///
/// A player can only be awarded once per run. If the add player is already on a
/// roster (or was claimed earlier in the same run), the claim is rejected.
/// Drop players are only required when the team's roster is full; this
/// implementation accepts the drop if specified, without enforcing max-roster
/// size (that enforcement can be added once RosterSettings slot counts are wired).
/// </summary>
public class WaiverProcessor : IWaiverProcessor
{
    private readonly CutlineDbContext _db;

    public WaiverProcessor(CutlineDbContext db) => _db = db;

    public async Task<WaiverProcessingResult> ProcessClaimsAsync(
        Guid leagueId, int weekNumber, CancellationToken ct = default)
    {
        var week = await _db.Weeks
            .Include(w => w.WaiverClaims)
            .FirstOrDefaultAsync(w => w.LeagueId == leagueId && w.WeekNumber == weekNumber, ct)
            ?? throw new InvalidOperationException($"Week {weekNumber} not found.");

        if (week.Status != WeekStatus.Eliminated)
            throw new InvalidOperationException(
                "Elimination must be run before processing waivers (week must be in Eliminated status).");

        var league = await _db.Leagues
            .FirstOrDefaultAsync(l => l.Id == leagueId, ct)
            ?? throw new InvalidOperationException("League not found.");

        var useFaab    = league.RosterSettings.UseFaab;
        var faabBudget = league.RosterSettings.FaabBudget;

        var pending = week.WaiverClaims
            .Where(wc => wc.Status == WaiverClaimStatus.Pending)
            .ToList();

        // Sort claims: FAAB leagues sort by highest bid (ties → lowest priority number);
        // standard waiver leagues sort by lowest priority number first.
        pending = useFaab
            ? pending.OrderByDescending(wc => wc.FaabBid ?? 0m).ThenBy(wc => wc.Priority).ToList()
            : pending.OrderBy(wc => wc.Priority).ToList();

        // Track state for this processing run
        var claimedThisRun    = new HashSet<Guid>();              // playerIds awarded so far
        var faabSpentThisRun  = new Dictionary<Guid, decimal>();  // teamId → bid total

        // Pre-load FAAB already spent by each team in prior waiver runs this season
        Dictionary<Guid, decimal> faabSpentPreviously = useFaab
            ? await _db.WaiverClaims
                .Where(wc => wc.Week.LeagueId == leagueId && wc.Status == WaiverClaimStatus.Processed)
                .GroupBy(wc => wc.TeamId)
                .Select(g => new { TeamId = g.Key, Total = g.Sum(wc => wc.FaabBid ?? 0m) })
                .ToDictionaryAsync(x => x.TeamId, x => x.Total, ct)
            : [];

        var outcomes = new List<WaiverClaimOutcome>();

        foreach (var claim in pending)
        {
            var (result, reason) = await EvaluateClaimAsync(
                claim, claimedThisRun, faabSpentThisRun, faabSpentPreviously,
                useFaab, faabBudget, ct);

            if (result == WaiverClaimStatus.Rejected)
            {
                claim.Status = WaiverClaimStatus.Rejected;
                claim.RejectionReason = reason;
                outcomes.Add(new WaiverClaimOutcome(
                    claim.Id, claim.TeamId, claim.AddPlayerId, claim.DropPlayerId,
                    WaiverClaimStatus.Rejected, reason));
                continue;
            }

            // --- Approved ---

            // Drop the player being released (if any)
            if (claim.DropPlayerId.HasValue)
            {
                var dropSlot = await _db.RosterSlots
                    .FirstOrDefaultAsync(rs => rs.PlayerId == claim.DropPlayerId && rs.TeamId == claim.TeamId, ct);
                if (dropSlot is not null)
                    _db.RosterSlots.Remove(dropSlot);
            }

            // Add the claimed player to bench
            await _db.RosterSlots.AddAsync(new RosterSlot
            {
                Id       = Guid.NewGuid(),
                TeamId   = claim.TeamId,
                PlayerId = claim.AddPlayerId,
                SlotType = SlotType.Bench,
                IsStarter = false,
            }, ct);

            claimedThisRun.Add(claim.AddPlayerId);

            if (useFaab)
                faabSpentThisRun[claim.TeamId] =
                    faabSpentThisRun.GetValueOrDefault(claim.TeamId, 0m) + (claim.FaabBid ?? 0m);

            claim.Status = WaiverClaimStatus.Processed;
            outcomes.Add(new WaiverClaimOutcome(
                claim.Id, claim.TeamId, claim.AddPlayerId, claim.DropPlayerId,
                WaiverClaimStatus.Processed, null));
        }

        week.Status = WeekStatus.Completed;
        await _db.SaveChangesAsync(ct);

        return new WaiverProcessingResult(
            outcomes.Count(o => o.Status == WaiverClaimStatus.Processed),
            outcomes.Count(o => o.Status == WaiverClaimStatus.Rejected),
            outcomes);
    }

    private async Task<(WaiverClaimStatus status, string? reason)> EvaluateClaimAsync(
        WaiverClaim claim,
        HashSet<Guid> claimedThisRun,
        Dictionary<Guid, decimal> faabSpentThisRun,
        Dictionary<Guid, decimal> faabSpentPreviously,
        bool useFaab, decimal faabBudget,
        CancellationToken ct)
    {
        if (claimedThisRun.Contains(claim.AddPlayerId))
            return (WaiverClaimStatus.Rejected, "Player already claimed by another team this run.");

        var isOnRoster = await _db.RosterSlots.AnyAsync(rs => rs.PlayerId == claim.AddPlayerId, ct);
        if (isOnRoster)
            return (WaiverClaimStatus.Rejected, "Player is no longer a free agent.");

        var team = await _db.Teams.FindAsync([claim.TeamId], ct);
        if (team is null || team.IsEliminated)
            return (WaiverClaimStatus.Rejected, "Claiming team has been eliminated.");

        if (useFaab)
        {
            var bid          = claim.FaabBid ?? 0m;
            var prevSpent    = faabSpentPreviously.GetValueOrDefault(claim.TeamId, 0m);
            var thisRunSpent = faabSpentThisRun.GetValueOrDefault(claim.TeamId, 0m);
            var remaining    = faabBudget - prevSpent - thisRunSpent;

            if (bid > remaining)
                return (WaiverClaimStatus.Rejected,
                    $"Insufficient FAAB — bid ${bid:F0}, remaining ${remaining:F0}.");
        }

        if (claim.DropPlayerId.HasValue)
        {
            var dropIsOwned = await _db.RosterSlots
                .AnyAsync(rs => rs.PlayerId == claim.DropPlayerId && rs.TeamId == claim.TeamId, ct);
            if (!dropIsOwned)
                return (WaiverClaimStatus.Rejected, "Drop player is not on your roster.");
        }

        return (WaiverClaimStatus.Processed, null);
    }
}
