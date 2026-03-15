namespace Cutline.Api.Endpoints;

using Cutline.Core.Entities;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public static class HistoryEndpoints
{
    public static void MapHistoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/leagues/{leagueId:guid}/history").RequireAuthorization();

        // GET /api/leagues/{leagueId}/history
        // Elimination timeline — every team cut, their week score, and what dropped to waivers.
        group.MapGet("/", async (Guid leagueId, CutlineDbContext db, CancellationToken ct) =>
        {
            var leagueExists = await db.Leagues.AnyAsync(l => l.Id == leagueId, ct);
            if (!leagueExists) return Results.NotFound();

            var weeks = await db.Weeks
                .Include(w => w.EliminatedTeam)
                .Include(w => w.TeamScores).ThenInclude(ts => ts.Team)
                .Include(w => w.WaiverClaims).ThenInclude(wc => wc.Team)
                .Where(w => w.LeagueId == leagueId && w.EliminatedTeamId != null)
                .OrderByDescending(w => w.WeekNumber)
                .ToListAsync(ct);

            if (weeks.Count == 0) return Results.Ok(Array.Empty<object>());

            // Load player details for all dropped players in one query
            var allDroppedIds = weeks.SelectMany(w => w.DroppedPlayerIds).Distinct().ToList();
            var players = allDroppedIds.Count > 0
                ? await db.Players
                    .Where(p => allDroppedIds.Contains(p.Id))
                    .Select(p => new { p.Id, p.FirstName, p.LastName, p.Position, p.NflTeam, p.Adp })
                    .ToDictionaryAsync(p => p.Id, ct)
                : [];

            var result = weeks.Select(w =>
            {
                var scores = w.TeamScores
                    .OrderByDescending(ts => ts.Points)
                    .Select((ts, i) => new
                    {
                        teamId   = ts.TeamId,
                        teamName = ts.Team?.Name ?? "Unknown",
                        points   = ts.Points,
                        rank     = i + 1,
                    })
                    .ToList();

                var losingScore = w.TeamScores
                    .FirstOrDefault(ts => ts.TeamId == w.EliminatedTeamId)?.Points;

                // The team that just barely survived (second lowest) determines the survival gap
                var activeScoresSorted = w.TeamScores
                    .OrderBy(ts => ts.Points)
                    .ToList();
                var survivorScore = activeScoresSorted.Count > 1
                    ? activeScoresSorted[1].Points
                    : (decimal?)null;
                var survivalGap = losingScore.HasValue && survivorScore.HasValue
                    ? survivorScore.Value - losingScore.Value
                    : (decimal?)null;

                // Build claimed-player lookup from processed claims this week
                var claimedMap = w.WaiverClaims
                    .Where(wc => wc.Status == WaiverClaimStatus.Processed)
                    .GroupBy(wc => wc.AddPlayerId)
                    .ToDictionary(g => g.Key, g => g.First());

                var droppedPlayers = w.DroppedPlayerIds
                    .Select(pid =>
                    {
                        players.TryGetValue(pid, out var p);
                        claimedMap.TryGetValue(pid, out var claim);
                        return new
                        {
                            player = p is null ? null : new
                            {
                                id        = p.Id,
                                firstName = p.FirstName,
                                lastName  = p.LastName,
                                position  = p.Position,
                                nflTeam   = p.NflTeam,
                                adp       = p.Adp,
                            },
                            claimedBy = claim is null ? null : new
                            {
                                id   = claim.Team.Id,
                                name = claim.Team.Name,
                            } as object,
                            faabBid = claim?.FaabBid,
                        };
                    })
                    // Sort: claimed first (by adp), then unclaimed
                    .OrderBy(d => d.claimedBy is null ? 1 : 0)
                    .ThenBy(d => d.player?.adp ?? 9999)
                    .ToList();

                return (object)new
                {
                    weekNumber  = w.WeekNumber,
                    weekStatus  = w.Status.ToString(),
                    eliminatedTeam = w.EliminatedTeam is null ? null : new
                    {
                        id   = w.EliminatedTeam.Id,
                        name = w.EliminatedTeam.Name,
                    },
                    losingScore,
                    survivalGap,
                    allScores    = scores,
                    droppedPlayers,
                };
            });

            return Results.Ok(result);
        });
    }
}
