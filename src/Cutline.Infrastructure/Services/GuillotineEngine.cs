namespace Cutline.Infrastructure.Services;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Core guillotine elimination logic.
///
/// Each week the team with the lowest total score among non-eliminated teams is
/// cut. Their roster is immediately released so other teams can submit waiver
/// claims before WaiverProcessor.ProcessClaimsAsync() runs.
///
/// Tiebreaker: lowest TeamId (deterministic but arbitrary). A configurable
/// tiebreaker (e.g. reverse waiver priority, season total) can be added later.
/// </summary>
public class GuillotineEngine : IGuillotineEngine
{
    private readonly CutlineDbContext _db;

    public GuillotineEngine(CutlineDbContext db) => _db = db;

    public async Task<EliminationResult> RunEliminationAsync(
        Guid leagueId, int weekNumber, CancellationToken ct = default)
    {
        var week = await _db.Weeks
            .Include(w => w.TeamScores)
            .FirstOrDefaultAsync(w => w.LeagueId == leagueId && w.WeekNumber == weekNumber, ct)
            ?? throw new InvalidOperationException($"Week {weekNumber} not found for league.");

        if (week.Status is WeekStatus.Eliminated or WeekStatus.Completed)
            throw new InvalidOperationException($"Week {weekNumber} has already been eliminated.");

        var activeTeams = await _db.Teams
            .Where(t => t.LeagueId == leagueId && !t.IsEliminated)
            .ToListAsync(ct);

        if (activeTeams.Count <= 1)
            throw new InvalidOperationException("Only one active team remains — the league is complete.");

        var activeTeamIds = activeTeams.Select(t => t.Id).ToHashSet();

        var lowestScore = week.TeamScores
            .Where(ts => activeTeamIds.Contains(ts.TeamId))
            .OrderBy(ts => ts.Points)
            .ThenBy(ts => ts.TeamId) // deterministic tiebreaker
            .FirstOrDefault()
            ?? throw new InvalidOperationException("No scores have been recorded for this week.");

        var eliminated = activeTeams.First(t => t.Id == lowestScore.TeamId);

        eliminated.IsEliminated = true;
        eliminated.EliminatedWeek = weekNumber;

        week.EliminatedTeamId = eliminated.Id;
        week.Status = WeekStatus.Eliminated;

        // Release roster — players with no RosterSlot are free agents available on waivers
        var releasedSlots = await _db.RosterSlots
            .Where(rs => rs.TeamId == eliminated.Id)
            .ToListAsync(ct);

        _db.RosterSlots.RemoveRange(releasedSlots);

        await _db.SaveChangesAsync(ct);

        return new EliminationResult(
            eliminated.Id,
            eliminated.Name,
            lowestScore.Points,
            weekNumber,
            releasedSlots.Count);
    }
}
