namespace Cutline.Api.Endpoints;

using Cutline.Core.Entities;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public static class TradeEndpoints
{
    public static void MapTradeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/leagues/{leagueId:guid}/trades").RequireAuthorization();

        // GET /api/leagues/{leagueId}/trades?teamId=
        // Returns all trades involving the given team, grouped by status.
        group.MapGet("/", async (Guid leagueId, Guid teamId, CutlineDbContext db, CancellationToken ct) =>
        {
            var teamExists = await db.Teams.AnyAsync(t => t.Id == teamId && t.LeagueId == leagueId, ct);
            if (!teamExists) return Results.NotFound();

            var trades = await db.Trades
                .Include(t => t.InitiatorTeam)
                .Include(t => t.ReceiverTeam)
                .Include(t => t.Items).ThenInclude(i => i.Player)
                .Where(t => t.LeagueId == leagueId &&
                            (t.InitiatorTeamId == teamId || t.ReceiverTeamId == teamId))
                .OrderByDescending(t => t.ProposedAt)
                .ToListAsync(ct);

            return Results.Ok(trades.Select(t => MapTrade(t)));
        });

        // POST /api/leagues/{leagueId}/trades
        // Propose a trade. Body includes both sides of the deal.
        group.MapPost("/", async (Guid leagueId, HttpContext ctx, ProposeTrade req, CutlineDbContext db, CancellationToken ct) =>
        {
            var jwtManagerId = AuthEndpoints.GetManagerId(ctx);
            if (req.InitiatorTeamId == req.ReceiverTeamId)
                return Results.BadRequest("Cannot trade with yourself.");

            if (req.OfferedPlayerIds.Count == 0 && req.RequestedPlayerIds.Count == 0)
                return Results.BadRequest("A trade must include at least one player.");

            // Load both teams scoped to the league
            var teams = await db.Teams
                .Where(t => t.LeagueId == leagueId &&
                            (t.Id == req.InitiatorTeamId || t.Id == req.ReceiverTeamId))
                .ToListAsync(ct);

            var initiator = teams.FirstOrDefault(t => t.Id == req.InitiatorTeamId);
            var receiver  = teams.FirstOrDefault(t => t.Id == req.ReceiverTeamId);

            if (initiator is null || receiver is null) return Results.NotFound();
            if (initiator.ManagerId != jwtManagerId) return Results.Forbid();
            if (initiator.IsEliminated) return Results.BadRequest("Eliminated teams cannot propose trades.");
            if (receiver.IsEliminated)  return Results.BadRequest("Cannot trade with an eliminated team.");
            if (initiator.IsLocked) return Results.BadRequest("Your team is locked by the commissioner.");
            if (receiver.IsLocked)  return Results.BadRequest("That team is locked by the commissioner.");

            // Verify offered players are on initiator's roster
            var allRequestedIds = req.OfferedPlayerIds.Concat(req.RequestedPlayerIds).Distinct().ToList();
            var slots = await db.RosterSlots
                .Where(rs => (rs.TeamId == req.InitiatorTeamId || rs.TeamId == req.ReceiverTeamId)
                             && rs.PlayerId != null)
                .Select(rs => new { rs.TeamId, rs.PlayerId })
                .ToListAsync(ct);

            var initiatorPlayerIds = slots.Where(s => s.TeamId == req.InitiatorTeamId)
                                          .Select(s => s.PlayerId!.Value).ToHashSet();
            var receiverPlayerIds  = slots.Where(s => s.TeamId == req.ReceiverTeamId)
                                          .Select(s => s.PlayerId!.Value).ToHashSet();

            foreach (var pid in req.OfferedPlayerIds)
                if (!initiatorPlayerIds.Contains(pid))
                    return Results.BadRequest($"Player {pid} is not on the initiator's roster.");

            foreach (var pid in req.RequestedPlayerIds)
                if (!receiverPlayerIds.Contains(pid))
                    return Results.BadRequest($"Player {pid} is not on the receiver's roster.");

            var items = req.OfferedPlayerIds
                .Select(pid => new TradeItem { Id = Guid.NewGuid(), PlayerId = pid, FromTeamId = req.InitiatorTeamId })
                .Concat(req.RequestedPlayerIds
                    .Select(pid => new TradeItem { Id = Guid.NewGuid(), PlayerId = pid, FromTeamId = req.ReceiverTeamId }))
                .ToList();

            var trade = new Trade
            {
                Id              = Guid.NewGuid(),
                LeagueId        = leagueId,
                InitiatorTeamId = req.InitiatorTeamId,
                ReceiverTeamId  = req.ReceiverTeamId,
                Message         = req.Message?.Trim() is { Length: > 0 } m ? m : null,
                ProposedAt      = DateTime.UtcNow,
                Items           = items,
            };

            db.Trades.Add(trade);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/leagues/{leagueId}/trades/{trade.Id}", new { tradeId = trade.Id });
        }).RequireAuthorization();

        // POST /api/leagues/{leagueId}/trades/{tradeId}/accept?teamId=
        group.MapPost("/{tradeId:guid}/accept", async (
            Guid leagueId, Guid tradeId, Guid teamId, HttpContext ctx, CutlineDbContext db, CancellationToken ct) =>
        {
            var jwtManagerId = AuthEndpoints.GetManagerId(ctx);
            var teamManagerId = await db.Teams.Where(t => t.Id == teamId && t.LeagueId == leagueId).Select(t => t.ManagerId).FirstOrDefaultAsync(ct);
            if (teamManagerId != jwtManagerId) return Results.Forbid();
            var trade = await db.Trades
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == tradeId && t.LeagueId == leagueId, ct);

            if (trade is null) return Results.NotFound();
            if (trade.ReceiverTeamId != teamId) return Results.Forbid();
            if (trade.Status != TradeStatus.Pending)
                return Results.BadRequest($"Trade is already {trade.Status}.");

            // Load all roster slots for both teams
            var allSlots = await db.RosterSlots
                .Where(rs => rs.TeamId == trade.InitiatorTeamId || rs.TeamId == trade.ReceiverTeamId)
                .ToListAsync(ct);

            var initiatorSlots = allSlots.Where(s => s.TeamId == trade.InitiatorTeamId).ToList();
            var receiverSlots  = allSlots.Where(s => s.TeamId == trade.ReceiverTeamId).ToList();

            // Step 1: Verify each player is still on the expected team and remove them
            foreach (var item in trade.Items)
            {
                var fromSlots = item.FromTeamId == trade.InitiatorTeamId ? initiatorSlots : receiverSlots;
                var slot = fromSlots.FirstOrDefault(s => s.PlayerId == item.PlayerId);
                if (slot is null)
                    return Results.BadRequest($"Player {item.PlayerId} is no longer on the expected roster — the trade may be stale.");
                slot.PlayerId = null;
            }

            // Step 2: Place each player on a bench slot of the receiving team.
            //         Trading away a player frees up their slot first, so process
            //         departures (above) before arrivals.
            foreach (var item in trade.Items)
            {
                var toTeamId  = item.FromTeamId == trade.InitiatorTeamId ? trade.ReceiverTeamId : trade.InitiatorTeamId;
                var toSlots   = toTeamId == trade.InitiatorTeamId ? initiatorSlots : receiverSlots;
                var benchSlot = toSlots.FirstOrDefault(s => s.SlotType == SlotType.Bench && s.PlayerId == null);
                if (benchSlot is null)
                    return Results.BadRequest($"No available bench slot on the receiving team. Free up a roster spot before accepting.");
                benchSlot.PlayerId = item.PlayerId;
            }

            trade.Status      = TradeStatus.Accepted;
            trade.RespondedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { tradeId = trade.Id, status = "Accepted" });
        }).RequireAuthorization();

        // POST /api/leagues/{leagueId}/trades/{tradeId}/reject?teamId=
        group.MapPost("/{tradeId:guid}/reject", async (
            Guid leagueId, Guid tradeId, Guid teamId, HttpContext ctx, CutlineDbContext db, CancellationToken ct) =>
        {
            var jwtManagerId = AuthEndpoints.GetManagerId(ctx);
            var teamManagerId = await db.Teams.Where(t => t.Id == teamId && t.LeagueId == leagueId).Select(t => t.ManagerId).FirstOrDefaultAsync(ct);
            if (teamManagerId != jwtManagerId) return Results.Forbid();
            var trade = await db.Trades
                .FirstOrDefaultAsync(t => t.Id == tradeId && t.LeagueId == leagueId, ct);

            if (trade is null) return Results.NotFound();
            if (trade.ReceiverTeamId != teamId) return Results.Forbid();
            if (trade.Status != TradeStatus.Pending)
                return Results.BadRequest($"Trade is already {trade.Status}.");

            trade.Status      = TradeStatus.Rejected;
            trade.RespondedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { tradeId = trade.Id, status = "Rejected" });
        }).RequireAuthorization();

        // DELETE /api/leagues/{leagueId}/trades/{tradeId}?teamId=
        // Cancel a pending trade (initiator only).
        group.MapDelete("/{tradeId:guid}", async (
            Guid leagueId, Guid tradeId, Guid teamId, HttpContext ctx, CutlineDbContext db, CancellationToken ct) =>
        {
            var jwtManagerId = AuthEndpoints.GetManagerId(ctx);
            var teamManagerId = await db.Teams.Where(t => t.Id == teamId && t.LeagueId == leagueId).Select(t => t.ManagerId).FirstOrDefaultAsync(ct);
            if (teamManagerId != jwtManagerId) return Results.Forbid();
            var trade = await db.Trades
                .FirstOrDefaultAsync(t => t.Id == tradeId && t.LeagueId == leagueId, ct);

            if (trade is null) return Results.NotFound();
            if (trade.InitiatorTeamId != teamId) return Results.Forbid();
            if (trade.Status != TradeStatus.Pending)
                return Results.BadRequest($"Trade is already {trade.Status}.");

            trade.Status      = TradeStatus.Cancelled;
            trade.RespondedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { tradeId = trade.Id, status = "Cancelled" });
        }).RequireAuthorization();
    }

    private static object MapTrade(Trade t) => new
    {
        id              = t.Id,
        status          = t.Status.ToString(),
        message         = t.Message,
        proposedAt      = t.ProposedAt,
        respondedAt     = t.RespondedAt,
        initiatorTeam   = new { id = t.InitiatorTeam.Id, name = t.InitiatorTeam.Name },
        receiverTeam    = new { id = t.ReceiverTeam.Id,  name = t.ReceiverTeam.Name },
        items           = t.Items.Select(i => new
        {
            playerId   = i.PlayerId,
            fromTeamId = i.FromTeamId,
            player     = i.Player is null ? null : new
            {
                id        = i.Player.Id,
                firstName = i.Player.FirstName,
                lastName  = i.Player.LastName,
                position  = i.Player.Position,
                nflTeam   = i.Player.NflTeam,
            },
        }),
    };
}

public record ProposeTrade(
    Guid InitiatorTeamId,
    Guid ReceiverTeamId,
    List<Guid> OfferedPlayerIds,
    List<Guid> RequestedPlayerIds,
    string? Message);
