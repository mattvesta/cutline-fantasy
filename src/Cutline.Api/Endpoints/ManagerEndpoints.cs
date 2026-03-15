namespace Cutline.Api.Endpoints;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public static class ManagerEndpoints
{
    public static void MapManagers(this WebApplication app)
    {
        // ── Global manager endpoints ───────────────────────────────────────
        var managers = app.MapGroup("/api/managers");

        managers.MapPost("/", async (CreateManagerRequest req, IManagerRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.DisplayName) || string.IsNullOrWhiteSpace(req.Email))
                return Results.BadRequest(new { message = "DisplayName and Email are required." });

            var existing = await repo.GetByEmailAsync(req.Email, ct);
            if (existing is not null)
                return Results.Conflict(new { message = "A manager with that email already exists." });

            var manager = new Manager
            {
                Id          = Guid.NewGuid(),
                DisplayName = req.DisplayName.Trim(),
                Email       = req.Email.Trim().ToLower(),
                AvatarUrl   = req.AvatarUrl,
                CreatedAt   = DateTime.UtcNow,
            };

            await repo.AddAsync(manager, ct);
            await repo.SaveChangesAsync(ct);

            return Results.Created($"/api/managers/{manager.Id}", manager);
        });

        managers.MapGet("/{managerId:guid}", async (Guid managerId, IManagerRepository repo, CancellationToken ct) =>
        {
            var manager = await repo.GetByIdAsync(managerId, ct);
            return manager is null ? Results.NotFound() : Results.Ok(manager);
        });

        managers.MapPut("/{managerId:guid}", async (Guid managerId, UpdateManagerRequest req, IManagerRepository repo, CancellationToken ct) =>
        {
            var manager = await repo.GetByIdAsync(managerId, ct);
            if (manager is null) return Results.NotFound();

            if (!string.IsNullOrWhiteSpace(req.DisplayName)) manager.DisplayName = req.DisplayName.Trim();
            if (req.AvatarUrl is not null) manager.AvatarUrl = req.AvatarUrl;

            await repo.SaveChangesAsync(ct);
            return Results.Ok(manager);
        });

        // ── League-scoped manager endpoints ───────────────────────────────
        var leagueManagers = app.MapGroup("/api/leagues/{leagueId:guid}/managers").RequireAuthorization();

        leagueManagers.MapGet("/", async (Guid leagueId, IManagerRepository repo, CancellationToken ct) =>
        {
            var memberships = await repo.GetLeagueMembershipsAsync(leagueId, ct);
            return Results.Ok(memberships);
        });

        // Add a manager to a league (join / invite accept)
        leagueManagers.MapPost("/", async (
            Guid leagueId,
            JoinLeagueRequest req,
            IManagerRepository repo,
            ILeagueRepository leagues,
            CancellationToken ct) =>
        {
            var league = await leagues.GetByIdAsync(leagueId, ct);
            if (league is null) return Results.NotFound(new { message = "League not found." });

            var manager = await repo.GetByIdAsync(req.ManagerId, ct);
            if (manager is null) return Results.NotFound(new { message = "Manager not found." });

            var existing = await repo.GetMembershipAsync(leagueId, req.ManagerId, ct);
            if (existing is not null)
                return Results.Conflict(new { message = "Manager is already in this league." });

            // First manager to join becomes commissioner automatically
            var memberships = await repo.GetLeagueMembershipsAsync(leagueId, ct);
            var isFirstMember = memberships.Count == 0;

            var membership = new LeagueManager
            {
                LeagueId       = leagueId,
                ManagerId      = req.ManagerId,
                IsCommissioner = isFirstMember,
                JoinedAt       = DateTime.UtcNow,
            };

            await repo.AddMembershipAsync(membership, ct);
            await repo.SaveChangesAsync(ct);

            return Results.Created($"/api/leagues/{leagueId}/managers/{req.ManagerId}", membership);
        });

        // Remove a manager from a league
        leagueManagers.MapDelete("/{managerId:guid}", async (
            Guid leagueId,
            Guid managerId,
            IManagerRepository repo,
            CutlineDbContext db,
            CancellationToken ct) =>
        {
            var membership = await repo.GetMembershipAsync(leagueId, managerId, ct);
            if (membership is null) return Results.NotFound();

            if (membership.IsCommissioner)
                return Results.BadRequest(new { message = "Cannot remove the commissioner. Assign a new commissioner first." });

            // Unassign their team in this league
            var team = await db.Teams.FirstOrDefaultAsync(t => t.LeagueId == leagueId && t.ManagerId == managerId, ct);
            if (team is not null) team.ManagerId = null;

            db.LeagueManagers.Remove(membership);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });

        // Assign a team to a manager within a league
        leagueManagers.MapPut("/{managerId:guid}/team", async (
            Guid leagueId,
            Guid managerId,
            AssignTeamRequest req,
            IManagerRepository repo,
            CutlineDbContext db,
            CancellationToken ct) =>
        {
            var membership = await repo.GetMembershipAsync(leagueId, managerId, ct);
            if (membership is null) return Results.NotFound(new { message = "Manager is not in this league." });

            var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == req.TeamId && t.LeagueId == leagueId, ct);
            if (team is null) return Results.NotFound(new { message = "Team not found in this league." });

            // Enforce: 1 team per manager per league (DB unique index handles it but give a clear message)
            var alreadyHasTeam = await db.Teams
                .AnyAsync(t => t.LeagueId == leagueId && t.ManagerId == managerId && t.Id != req.TeamId, ct);
            if (alreadyHasTeam)
                return Results.Conflict(new { message = "Manager already has a team in this league." });

            // Unassign from the team's current manager
            if (team.ManagerId.HasValue && team.ManagerId != managerId)
            {
                var prev = await db.Teams.FirstOrDefaultAsync(t => t.ManagerId == managerId && t.LeagueId == leagueId, ct);
                if (prev is not null) prev.ManagerId = null;
            }

            team.ManagerId = managerId;
            await db.SaveChangesAsync(ct);

            return Results.Ok(team);
        });

        // Set commissioner — only one per league
        leagueManagers.MapPut("/{managerId:guid}/commissioner", async (
            Guid leagueId,
            Guid managerId,
            IManagerRepository repo,
            CutlineDbContext db,
            CancellationToken ct) =>
        {
            var membership = await repo.GetMembershipAsync(leagueId, managerId, ct);
            if (membership is null) return Results.NotFound(new { message = "Manager is not in this league." });

            // Demote current commissioner
            var current = await db.LeagueManagers
                .FirstOrDefaultAsync(lm => lm.LeagueId == leagueId && lm.IsCommissioner, ct);
            if (current is not null) current.IsCommissioner = false;

            membership.IsCommissioner = true;
            await db.SaveChangesAsync(ct);

            return Results.Ok(membership);
        });
    }
}

public record CreateManagerRequest(string DisplayName, string Email, string? AvatarUrl = null);
public record UpdateManagerRequest(string? DisplayName, string? AvatarUrl);
public record JoinLeagueRequest(Guid ManagerId);
public record AssignTeamRequest(Guid TeamId);
