namespace Cutline.Api.Endpoints;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public static class ScoringEndpoints
{
    public static void MapScoringEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/leagues/{leagueId:guid}/scoring");

        // GET /api/leagues/{leagueId}/scoring/live
        group.MapGet("/live", async (
            Guid leagueId,
            ILeagueRepository leagues,
            ILiveScoringService scoring,
            CutlineDbContext db,
            CancellationToken ct) =>
        {
            var league = await leagues.GetByIdAsync(leagueId, ct);
            if (league is null) return Results.NotFound();

            var week = await db.Weeks
                .Where(w => w.LeagueId == leagueId &&
                            (w.Status == WeekStatus.InProgress || w.Status == WeekStatus.Completed))
                .OrderByDescending(w => w.WeekNumber)
                .FirstOrDefaultAsync(ct)
                ?? await db.Weeks
                    .Where(w => w.LeagueId == leagueId)
                    .OrderByDescending(w => w.WeekNumber)
                    .FirstOrDefaultAsync(ct);

            if (week is null) return Results.NotFound(new { message = "No weeks found for this league." });

            var teamScores = new List<object>();
            foreach (var team in league.Teams)
            {
                var pts = await scoring.GetTeamStarterPointsAsync(team.Id, league.Season, week.WeekNumber, ct);
                teamScores.Add(new { teamId = team.Id, teamName = team.Name, points = pts });
            }

            return Results.Ok(new
            {
                leagueId,
                season     = league.Season,
                weekNumber = week.WeekNumber,
                teams      = teamScores,
            });
        });

        // GET /api/leagues/{leagueId}/scoring/matchup
        // Full weekly matchup view: all teams sorted by live points, cut line identified.
        group.MapGet("/matchup", async (
            Guid leagueId,
            ILeagueRepository leagues,
            ILiveScoringService scoring,
            CutlineDbContext db,
            CancellationToken ct) =>
        {
            var league = await leagues.GetByIdAsync(leagueId, ct);
            if (league is null) return Results.NotFound();

            var week = await db.Weeks
                .Where(w => w.LeagueId == leagueId &&
                            (w.Status == WeekStatus.InProgress || w.Status == WeekStatus.Completed))
                .OrderByDescending(w => w.WeekNumber)
                .FirstOrDefaultAsync(ct)
                ?? await db.Weeks
                    .Where(w => w.LeagueId == leagueId)
                    .OrderByDescending(w => w.WeekNumber)
                    .FirstOrDefaultAsync(ct);

            if (week is null) return Results.NotFound(new { message = "No weeks found." });

            var activeTeams   = league.Teams.Where(t => !t.IsEliminated).ToList();
            var eliminatedTeams = league.Teams.Where(t => t.IsEliminated)
                                              .OrderByDescending(t => t.EliminatedWeek)
                                              .ToList();

            // Build live scores for active teams
            var scores = new List<(Guid teamId, string teamName, string? managerName, decimal points)>();
            foreach (var t in activeTeams)
            {
                var pts = await scoring.GetTeamStarterPointsAsync(t.Id, league.Season, week.WeekNumber, ct);
                var mgr = t.Manager?.DisplayName;
                scores.Add((t.Id, t.Name, mgr, pts));
            }

            scores = scores.OrderByDescending(s => s.points).ToList();

            var activeRows = scores.Select((s, i) => new
            {
                teamId      = s.teamId,
                teamName    = s.teamName,
                managerName = s.managerName,
                points      = s.points,
                rank        = i + 1,
                isEliminated = false,
                isOnCutLine = i == scores.Count - 1 && scores.Count > 1,
            }).ToList<object>();

            var eliminatedRows = eliminatedTeams.Select(t => (object)new
            {
                teamId       = t.Id,
                teamName     = t.Name,
                managerName  = t.Manager?.DisplayName,
                points       = (decimal?)null,
                rank         = (int?)null,
                isEliminated = true,
                isOnCutLine  = false,
                eliminatedWeek = t.EliminatedWeek,
            }).ToList();

            return Results.Ok(new
            {
                leagueId,
                season     = league.Season,
                weekNumber = week.WeekNumber,
                weekStatus = week.Status.ToString(),
                teams      = activeRows.Concat(eliminatedRows),
            });
        });

        // GET /api/leagues/{leagueId}/scoring/teams/{teamId}
        group.MapGet("/teams/{teamId:guid}", async (
            Guid leagueId,
            Guid teamId,
            ILeagueRepository leagues,
            ILiveScoringService scoring,
            CutlineDbContext db,
            CancellationToken ct) =>
        {
            var league = await leagues.GetByIdAsync(leagueId, ct);
            if (league is null) return Results.NotFound();

            var team = league.Teams.FirstOrDefault(t => t.Id == teamId);
            if (team is null) return Results.NotFound();

            var week = await db.Weeks
                .Where(w => w.LeagueId == leagueId &&
                            (w.Status == WeekStatus.InProgress || w.Status == WeekStatus.Completed))
                .OrderByDescending(w => w.WeekNumber)
                .FirstOrDefaultAsync(ct)
                ?? await db.Weeks
                    .Where(w => w.LeagueId == leagueId)
                    .OrderByDescending(w => w.WeekNumber)
                    .FirstOrDefaultAsync(ct);

            var slots = await db.RosterSlots
                .Where(rs => rs.TeamId == teamId && rs.PlayerId != null)
                .Include(rs => rs.Player)
                .ToListAsync(ct);

            // If no week yet, return roster with no stats
            Dictionary<Guid, PlayerGameStats> statsMap = new();
            if (week is not null)
            {
                var playerIds = slots.Select(rs => rs.PlayerId!.Value).Distinct().ToList();
                statsMap = (await scoring.GetPlayerStatsAsync(league.Season, week.WeekNumber, playerIds, ct))
                           .ToDictionary(s => s.PlayerId);
            }

            var roster = slots.Select(rs =>
            {
                statsMap.TryGetValue(rs.PlayerId!.Value, out var stats);
                return new
                {
                    slotId    = rs.Id,
                    slotType  = rs.SlotType.ToString(),
                    isStarter = rs.IsStarter,
                    player    = rs.Player is null ? null : new
                    {
                        id       = rs.Player.Id,
                        name     = $"{rs.Player.FirstName} {rs.Player.LastName}",
                        position = rs.Player.Position,
                        team     = rs.Player.NflTeam,
                    },
                    stats = stats is null ? null : new
                    {
                        points             = stats.Points,
                        gameStatus         = stats.GameStatus.ToString(),
                        opponent           = stats.Opponent,
                        lastUpdated        = stats.LastUpdated,
                        passingYards       = stats.PassingYards,
                        passingTDs         = stats.PassingTDs,
                        interceptions      = stats.Interceptions,
                        passingAttempts    = stats.PassingAttempts,
                        passingCompletions = stats.PassingCompletions,
                        rushingYards       = stats.RushingYards,
                        rushingTDs         = stats.RushingTDs,
                        rushingAttempts    = stats.RushingAttempts,
                        fumbles            = stats.Fumbles,
                        receptions         = stats.Receptions,
                        targets            = stats.Targets,
                        receivingYards     = stats.ReceivingYards,
                        receivingTDs       = stats.ReceivingTDs,
                        fieldGoalsMade     = stats.FieldGoalsMade,
                        fieldGoalsAttempted= stats.FieldGoalsAttempted,
                        longFieldGoal      = stats.LongFieldGoal,
                        extraPointsMade    = stats.ExtraPointsMade,
                        sacks              = stats.Sacks,
                        defensiveInts      = stats.DefensiveInterceptions,
                        fumblesRecovered   = stats.FumblesRecovered,
                        defensiveTDs       = stats.DefensiveTDs,
                        pointsAllowed      = stats.PointsAllowed,
                        safeties           = stats.Safeties,
                    },
                };
            }).ToList();

            var starterPoints = roster
                .Where(r => r.isStarter && r.stats != null)
                .Sum(r => r.stats!.points);

            return Results.Ok(new
            {
                teamId,
                teamName    = team.Name,
                season      = league.Season,
                weekNumber  = week?.WeekNumber,
                totalPoints = starterPoints,
                roster,
            });
        });
    }
}
