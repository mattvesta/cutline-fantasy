namespace Cutline.Api.Endpoints;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public static class TeamEndpoints
{
    public static RouteGroupBuilder MapTeams(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leagues/{leagueId:guid}/teams").RequireAuthorization();

        group.MapGet("/", async (Guid leagueId, ITeamRepository teams, CancellationToken ct) =>
        {
            var all = await teams.GetByLeagueAsync(leagueId, ct);
            return Results.Ok(all);
        });

        group.MapGet("/{teamId:guid}", async (Guid leagueId, Guid teamId, ITeamRepository teams, CancellationToken ct) =>
        {
            var team = await teams.GetByIdAsync(leagueId, teamId, ct);
            return team is null ? Results.NotFound() : Results.Ok(team);
        });

        group.MapPost("/", async (Guid leagueId, CreateTeamRequest req, ILeagueRepository leagues, ITeamRepository teams, CancellationToken ct) =>
        {
            var league = await leagues.GetByIdAsync(leagueId, ct);
            if (league is null) return Results.NotFound();

            var team = new Team
            {
                Id          = Guid.NewGuid(),
                LeagueId    = leagueId,
                Name        = req.Name,
                OwnerUserId = req.OwnerUserId,
            };

            await teams.AddAsync(team, ct);
            await teams.SaveChangesAsync(ct);

            return Results.Created($"/api/leagues/{leagueId}/teams/{team.Id}", team);
        });

        // Swap two roster slots (drag-and-drop lineup changes)
        group.MapPost("/{teamId:guid}/lineup/swap", async (
            Guid leagueId, Guid teamId,
            HttpContext ctx,
            SwapSlotsRequest req,
            CutlineDbContext db,
            ITeamRepository teams,
            CancellationToken ct) =>
        {
            var jwtManagerId = AuthEndpoints.GetManagerId(ctx);
            var teamInfo = await db.Teams.Where(t => t.Id == teamId && t.LeagueId == leagueId)
                .Select(t => new { t.IsLocked, t.ManagerId }).FirstOrDefaultAsync(ct);
            if (teamInfo is null) return Results.NotFound();
            if (teamInfo.ManagerId != jwtManagerId) return Results.Forbid();
            if (teamInfo.IsLocked) return Results.BadRequest(new { error = "This team is locked by the commissioner." });

            var slotA = await db.RosterSlots.FirstOrDefaultAsync(s => s.Id == req.SlotAId && s.TeamId == teamId, ct);
            var slotB = await db.RosterSlots.FirstOrDefaultAsync(s => s.Id == req.SlotBId && s.TeamId == teamId, ct);

            if (slotA is null || slotB is null) return Results.NotFound();

            // Always swap PlayerIds — if one slot is empty (null) this becomes a move
            (slotA.PlayerId, slotB.PlayerId) = (slotB.PlayerId, slotA.PlayerId);

            await db.SaveChangesAsync(ct);

            var updated = await teams.GetByIdAsync(leagueId, teamId, ct);
            return Results.Ok(updated);
        }).RequireAuthorization();

        return group;
    }
}

public record CreateTeamRequest(string Name, string OwnerUserId);
public record SwapSlotsRequest(Guid SlotAId, Guid SlotBId);
