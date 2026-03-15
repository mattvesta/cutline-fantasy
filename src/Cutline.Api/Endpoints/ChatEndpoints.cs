namespace Cutline.Api.Endpoints;

using Cutline.Api.Hubs;
using Cutline.Core.Entities;
using Cutline.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/leagues/{leagueId:guid}/chat").RequireAuthorization();

        // GET /api/leagues/{leagueId}/chat?before=&limit=50
        // Returns messages in ascending (oldest-first) order for display.
        // Use `before` (ISO datetime) for cursor-based pagination when scrolling up.
        group.MapGet("/", async (
            Guid leagueId,
            CutlineDbContext db,
            CancellationToken ct,
            DateTime? before = null,
            int limit = 50) =>
        {
            var leagueExists = await db.Leagues.AnyAsync(l => l.Id == leagueId, ct);
            if (!leagueExists) return Results.NotFound();

            limit = Math.Clamp(limit, 1, 100);

            var query = db.ChatMessages
                .Include(m => m.Manager)
                .Where(m => m.LeagueId == leagueId);

            if (before.HasValue)
                query = query.Where(m => m.SentAt < before.Value);

            var messages = await query
                .OrderByDescending(m => m.SentAt)
                .Take(limit)
                .ToListAsync(ct);

            messages.Reverse(); // ascending for client
            return Results.Ok(messages.Select(MapMessage));
        });

        // POST /api/leagues/{leagueId}/chat
        group.MapPost("/", async (
            Guid leagueId,
            HttpContext ctx,
            SendChatRequest req,
            CutlineDbContext db,
            IHubContext<ChatHub> hub,
            CancellationToken ct) =>
        {
            var managerId = AuthEndpoints.GetManagerId(ctx);
            if (managerId == Guid.Empty) return Results.Unauthorized();

            var inLeague = await db.LeagueManagers.AnyAsync(
                lm => lm.LeagueId == leagueId && lm.ManagerId == managerId, ct);
            if (!inLeague) return Results.Forbid();

            var hasContent = !string.IsNullOrWhiteSpace(req.Content);
            var hasGif     = !string.IsNullOrWhiteSpace(req.GifUrl);
            if (!hasContent && !hasGif)
                return Results.BadRequest(new { error = "Message cannot be empty." });

            var manager = await db.Managers.FirstOrDefaultAsync(m => m.Id == managerId, ct);
            if (manager is null) return Results.NotFound();

            var message = new ChatMessage
            {
                Id        = Guid.NewGuid(),
                LeagueId  = leagueId,
                ManagerId = managerId,
                Content   = req.Content?.Trim() ?? string.Empty,
                GifUrl    = hasGif ? req.GifUrl!.Trim() : null,
                SentAt    = DateTime.UtcNow,
            };

            db.ChatMessages.Add(message);
            await db.SaveChangesAsync(ct);

            message.Manager = manager;
            var dto = MapMessage(message);

            await hub.Clients.Group($"chat:{leagueId}").SendAsync("NewMessage", dto, ct);
            return Results.Created($"/api/leagues/{leagueId}/chat/{message.Id}", dto);
        }).RequireAuthorization();
    }

    private static object MapMessage(ChatMessage m) => new
    {
        id          = m.Id,
        managerId   = m.ManagerId,
        managerName = m.Manager?.DisplayName ?? "Unknown",
        content     = m.Content,
        gifUrl      = m.GifUrl,
        sentAt      = m.SentAt,
    };
}

public record SendChatRequest(string? Content, string? GifUrl);
