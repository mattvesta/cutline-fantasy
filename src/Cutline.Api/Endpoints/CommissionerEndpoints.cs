namespace Cutline.Api.Endpoints;

using Cutline.Core.Entities;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public static class CommissionerEndpoints
{
    public static void MapCommissionerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/leagues/{leagueId:guid}/commissioner")
                       .RequireAuthorization();

        // GET /api/leagues/{leagueId}/commissioner
        group.MapGet("/", async (
            Guid leagueId, HttpContext ctx, CutlineDbContext db, CancellationToken ct) =>
        {
            var managerId = AuthEndpoints.GetManagerId(ctx);
            if (!await IsCommissioner(db, leagueId, managerId, ct)) return Results.Forbid();

            var league = await db.Leagues.FirstOrDefaultAsync(l => l.Id == leagueId, ct);
            if (league is null) return Results.NotFound();

            var teams = await db.Teams
                .Where(t => t.LeagueId == leagueId)
                .Include(t => t.Manager)
                .OrderBy(t => t.Name)
                .ToListAsync(ct);

            var weeks = await db.Weeks
                .Include(w => w.TeamScores).ThenInclude(ts => ts.Team)
                .Where(w => w.LeagueId == leagueId)
                .OrderBy(w => w.WeekNumber)
                .ToListAsync(ct);

            return Results.Ok(new
            {
                name            = league.Name,
                status          = league.Status.ToString(),
                receptionPoints = league.ScoringSettings.ReceptionPoints,
                useFaab         = league.RosterSettings.UseFaab,
                faabBudget      = league.RosterSettings.FaabBudget,
                minFaabBid      = league.RosterSettings.MinFaabBid,
                teams = teams.Select(t => new
                {
                    id             = t.Id,
                    name           = t.Name,
                    managerName    = t.Manager?.DisplayName,
                    isEliminated   = t.IsEliminated,
                    eliminatedWeek = t.EliminatedWeek,
                    isLocked       = t.IsLocked,
                }),
                weeks = weeks.Select(w => new
                {
                    weekNumber = w.WeekNumber,
                    status     = w.Status.ToString(),
                    scores     = w.TeamScores.Select(ts => new
                    {
                        teamId   = ts.TeamId,
                        teamName = ts.Team?.Name ?? "Unknown",
                        points   = ts.Points,
                        isLocked = ts.IsLocked,
                    }),
                }),
            });
        });

        // PATCH /api/leagues/{leagueId}/commissioner/settings
        group.MapPatch("/settings", async (
            Guid leagueId, HttpContext ctx, UpdateSettingsRequest req, CutlineDbContext db, CancellationToken ct) =>
        {
            var managerId = AuthEndpoints.GetManagerId(ctx);
            if (!await IsCommissioner(db, leagueId, managerId, ct)) return Results.Forbid();

            var league = await db.Leagues.FirstOrDefaultAsync(l => l.Id == leagueId, ct);
            if (league is null) return Results.NotFound();

            if (req.Name is { Length: > 0 } name) league.Name = name.Trim();
            if (req.ReceptionPoints is not null) league.ScoringSettings.ReceptionPoints = req.ReceptionPoints.Value;
            if (req.UseFaab is not null)         league.RosterSettings.UseFaab          = req.UseFaab.Value;
            if (req.FaabBudget is not null)      league.RosterSettings.FaabBudget       = req.FaabBudget.Value;
            if (req.MinFaabBid is not null)      league.RosterSettings.MinFaabBid       = req.MinFaabBid.Value;

            await db.SaveChangesAsync(ct);
            return Results.Ok(new
            {
                name            = league.Name,
                receptionPoints = league.ScoringSettings.ReceptionPoints,
                useFaab         = league.RosterSettings.UseFaab,
                faabBudget      = league.RosterSettings.FaabBudget,
                minFaabBid      = league.RosterSettings.MinFaabBid,
            });
        });

        // PATCH /api/leagues/{leagueId}/commissioner/weeks/{weekNumber}/status
        group.MapPatch("/weeks/{weekNumber:int}/status", async (
            Guid leagueId, int weekNumber, HttpContext ctx, SetWeekStatusRequest req, CutlineDbContext db, CancellationToken ct) =>
        {
            var managerId = AuthEndpoints.GetManagerId(ctx);
            if (!await IsCommissioner(db, leagueId, managerId, ct)) return Results.Forbid();

            var week = await db.Weeks.FirstOrDefaultAsync(
                w => w.LeagueId == leagueId && w.WeekNumber == weekNumber, ct);
            if (week is null) return Results.NotFound();

            week.Status = req.Status;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { weekNumber, status = week.Status.ToString() });
        });

        // PUT /api/leagues/{leagueId}/commissioner/weeks/{weekNumber}/scores/{teamId}
        group.MapPut("/weeks/{weekNumber:int}/scores/{teamId:guid}", async (
            Guid leagueId, int weekNumber, Guid teamId,
            HttpContext ctx, OverrideScoreRequest req, CutlineDbContext db, CancellationToken ct) =>
        {
            var managerId = AuthEndpoints.GetManagerId(ctx);
            if (!await IsCommissioner(db, leagueId, managerId, ct)) return Results.Forbid();

            var week = await db.Weeks
                .Include(w => w.TeamScores)
                .FirstOrDefaultAsync(w => w.LeagueId == leagueId && w.WeekNumber == weekNumber, ct);
            if (week is null) return Results.NotFound();

            var teamExists = await db.Teams.AnyAsync(t => t.Id == teamId && t.LeagueId == leagueId, ct);
            if (!teamExists) return Results.NotFound();

            var existing = week.TeamScores.FirstOrDefault(ts => ts.TeamId == teamId);
            if (existing is not null)
            {
                existing.Points = req.Points;
            }
            else
            {
                week.TeamScores.Add(new TeamScore
                {
                    Id       = Guid.NewGuid(),
                    WeekId   = week.Id,
                    TeamId   = teamId,
                    Points   = req.Points,
                    IsLocked = false,
                });
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(new { teamId, weekNumber, points = req.Points });
        });

        // POST /api/leagues/{leagueId}/commissioner/teams/{teamId}/eliminate
        group.MapPost("/teams/{teamId:guid}/eliminate", async (
            Guid leagueId, Guid teamId, HttpContext ctx, ForceEliminateRequest req, CutlineDbContext db, CancellationToken ct) =>
        {
            var managerId = AuthEndpoints.GetManagerId(ctx);
            if (!await IsCommissioner(db, leagueId, managerId, ct)) return Results.Forbid();

            var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == teamId && t.LeagueId == leagueId, ct);
            if (team is null) return Results.NotFound();
            if (team.IsEliminated) return Results.BadRequest(new { error = "Team is already eliminated." });

            var week = await db.Weeks
                .Include(w => w.TeamScores)
                .FirstOrDefaultAsync(w => w.LeagueId == leagueId && w.WeekNumber == req.WeekNumber, ct);
            if (week is null)
                return Results.BadRequest(new { error = $"Week {req.WeekNumber} not found." });
            if (week.EliminatedTeamId is not null)
                return Results.BadRequest(new { error = $"Week {req.WeekNumber} already has an eliminated team." });

            team.IsEliminated   = true;
            team.EliminatedWeek = req.WeekNumber;
            week.EliminatedTeamId = teamId;
            week.Status = WeekStatus.Eliminated;

            var releasedSlots = await db.RosterSlots
                .Where(rs => rs.TeamId == teamId && rs.PlayerId != null)
                .ToListAsync(ct);
            week.DroppedPlayerIds = releasedSlots.Select(rs => rs.PlayerId!.Value).ToArray();
            db.RosterSlots.RemoveRange(releasedSlots);

            await db.SaveChangesAsync(ct);
            return Results.Ok(new
            {
                teamId,
                teamName        = team.Name,
                weekNumber      = req.WeekNumber,
                playersReleased = releasedSlots.Count,
            });
        });

        // POST /api/leagues/{leagueId}/commissioner/teams/{teamId}/restore
        group.MapPost("/teams/{teamId:guid}/restore", async (
            Guid leagueId, Guid teamId, HttpContext ctx, CutlineDbContext db, CancellationToken ct) =>
        {
            var managerId = AuthEndpoints.GetManagerId(ctx);
            if (!await IsCommissioner(db, leagueId, managerId, ct)) return Results.Forbid();

            var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == teamId && t.LeagueId == leagueId, ct);
            if (team is null) return Results.NotFound();
            if (!team.IsEliminated) return Results.BadRequest(new { error = "Team is not eliminated." });

            var elimWeek = await db.Weeks
                .FirstOrDefaultAsync(w => w.LeagueId == leagueId && w.EliminatedTeamId == teamId, ct);
            if (elimWeek is not null)
            {
                elimWeek.EliminatedTeamId = null;
                elimWeek.Status           = WeekStatus.Scoring;
            }

            team.IsEliminated   = false;
            team.EliminatedWeek = null;

            await db.SaveChangesAsync(ct);
            return Results.Ok(new { teamId, teamName = team.Name, restored = true });
        });

        // PATCH /api/leagues/{leagueId}/commissioner/teams/{teamId}/lock
        group.MapPatch("/teams/{teamId:guid}/lock", async (
            Guid leagueId, Guid teamId, HttpContext ctx, SetTeamLockRequest req, CutlineDbContext db, CancellationToken ct) =>
        {
            var managerId = AuthEndpoints.GetManagerId(ctx);
            if (!await IsCommissioner(db, leagueId, managerId, ct)) return Results.Forbid();

            var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == teamId && t.LeagueId == leagueId, ct);
            if (team is null) return Results.NotFound();

            team.IsLocked = req.IsLocked;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { teamId, isLocked = team.IsLocked });
        });
    }

    private static Task<bool> IsCommissioner(CutlineDbContext db, Guid leagueId, Guid managerId, CancellationToken ct) =>
        db.LeagueManagers.AnyAsync(
            lm => lm.LeagueId == leagueId && lm.ManagerId == managerId && lm.IsCommissioner, ct);
}

public record UpdateSettingsRequest(string? Name, decimal? ReceptionPoints, bool? UseFaab, decimal? FaabBudget, decimal? MinFaabBid);
public record SetWeekStatusRequest(WeekStatus Status);
public record OverrideScoreRequest(decimal Points);
public record ForceEliminateRequest(int WeekNumber);
public record SetTeamLockRequest(bool IsLocked);
