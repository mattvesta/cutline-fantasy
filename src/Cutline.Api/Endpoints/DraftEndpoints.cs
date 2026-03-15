namespace Cutline.Api.Endpoints;

using Cutline.Core.Interfaces;
using Cutline.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

public static class DraftEndpoints
{
    public static void MapDraftEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/leagues/{leagueId:guid}/draft");

        // GET /api/leagues/{leagueId}/draft
        group.MapGet("", async (Guid leagueId, IDraftService svc, CancellationToken ct) =>
        {
            var draft = await svc.GetByLeagueAsync(leagueId, ct);
            return draft is null ? Results.NotFound() : Results.Ok(draft);
        });

        // POST /api/leagues/{leagueId}/draft  — create draft
        group.MapPost("", async (Guid leagueId, IDraftService svc, CancellationToken ct) =>
        {
            try
            {
                var draft = await svc.CreateAsync(leagueId, ct);
                return Results.Created($"/api/leagues/{leagueId}/draft", draft);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        // POST /api/leagues/{leagueId}/draft/start
        group.MapPost("/start", async (Guid leagueId, IDraftService svc, IHubContext<DraftHub> hub, CancellationToken ct) =>
        {
            var draft = await svc.GetByLeagueAsync(leagueId, ct);
            if (draft is null) return Results.NotFound();

            try
            {
                draft = await svc.StartAsync(draft.Id, ct);
                await hub.Clients.Group($"draft:{draft.Id}").SendAsync("DraftStarted", draft, ct);
                return Results.Ok(draft);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        // POST /api/leagues/{leagueId}/draft/pick
        group.MapPost("/pick", async (Guid leagueId, PickRequest req, IDraftService svc, IHubContext<DraftHub> hub, CancellationToken ct) =>
        {
            var draft = await svc.GetByLeagueAsync(leagueId, ct);
            if (draft is null) return Results.NotFound();

            try
            {
                var (pick, updatedDraft) = await svc.MakePickAsync(draft.Id, req.TeamId, req.PlayerId, ct: ct);
                await hub.Clients.Group($"draft:{draft.Id}").SendAsync("PickMade", new { pick, draft = updatedDraft }, ct);
                return Results.Ok(new { pick, draft = updatedDraft });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        // POST /api/leagues/{leagueId}/draft/autopick
        group.MapPost("/autopick", async (Guid leagueId, IDraftService svc, IHubContext<DraftHub> hub, CancellationToken ct) =>
        {
            var draft = await svc.GetByLeagueAsync(leagueId, ct);
            if (draft is null) return Results.NotFound();

            try
            {
                var (pick, updatedDraft) = await svc.AutoPickAsync(draft.Id, ct);
                await hub.Clients.Group($"draft:{updatedDraft.Id}").SendAsync("PickMade", new { pick, draft = updatedDraft }, ct);
                return Results.Ok(new { pick, draft = updatedDraft });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        // GET /api/leagues/{leagueId}/draft/available
        group.MapGet("/available", async (Guid leagueId, IDraftService svc, CancellationToken ct) =>
        {
            var draft = await svc.GetByLeagueAsync(leagueId, ct);
            if (draft is null) return Results.NotFound();

            var players = await svc.GetAvailablePlayersAsync(draft.Id, ct);
            return Results.Ok(players);
        });
    }
}

public record PickRequest(Guid TeamId, Guid PlayerId);
