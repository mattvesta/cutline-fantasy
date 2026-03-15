namespace Cutline.Api.Endpoints;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public static class WaiverEndpoints
{
    private const int PageSize = 30;

    public static void MapWaiverEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/leagues/{leagueId:guid}/waivers");

        // GET /api/leagues/{leagueId}/waivers?teamId=&position=&search=&page=
        // Full waiver wire state: available free agents, pending claims, recent results.
        group.MapGet("/", async (
            Guid leagueId,
            Guid? teamId,
            string? position,
            string? search,
            ILeagueRepository leagues,
            CutlineDbContext db,
            CancellationToken ct,
            int page = 1) =>
        {
            page = Math.Max(1, page);

            var league = await leagues.GetByIdAsync(leagueId, ct);
            if (league is null) return Results.NotFound();

            // Current open waiver window week
            var currentWeek = await db.Weeks
                .Include(w => w.WaiverClaims).ThenInclude(wc => wc.AddPlayer)
                .Include(w => w.WaiverClaims).ThenInclude(wc => wc.DropPlayer)
                .Where(w => w.LeagueId == leagueId &&
                            (w.Status == WeekStatus.Eliminated ||
                             w.Status == WeekStatus.InProgress  ||
                             w.Status == WeekStatus.Scoring))
                .OrderByDescending(w => w.WeekNumber)
                .FirstOrDefaultAsync(ct);

            // Players already on a roster in this league
            var teamIds    = league.Teams.Select(t => t.Id).ToList();
            var rosteredIds = await db.RosterSlots
                .Where(rs => rs.PlayerId != null && teamIds.Contains(rs.TeamId))
                .Select(rs => rs.PlayerId!.Value)
                .Distinct()
                .ToListAsync(ct);

            // Available free agents
            var query = db.Players.Where(p => !rosteredIds.Contains(p.Id));
            if (!string.IsNullOrWhiteSpace(position) && position != "ALL")
                query = query.Where(p => p.Position == position);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.FirstName.Contains(search) || p.LastName.Contains(search));

            var totalCount = await query.CountAsync(ct);
            var players    = await query
                .OrderBy(p => p.Adp ?? 9999)
                .ThenBy(p => p.LastName)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(p => new { p.Id, p.FirstName, p.LastName, p.Position, p.NflTeam, p.Status, p.Adp, p.ByeWeek })
                .ToListAsync(ct);

            // FAAB remaining for this team
            decimal faabRemaining = league.RosterSettings.FaabBudget;
            if (teamId.HasValue && league.RosterSettings.UseFaab)
            {
                var spent = await db.WaiverClaims
                    .Where(wc => wc.TeamId == teamId.Value &&
                                 wc.Status == WaiverClaimStatus.Processed &&
                                 wc.Week.LeagueId == leagueId)
                    .SumAsync(wc => wc.FaabBid ?? 0m, ct);
                faabRemaining = league.RosterSettings.FaabBudget - spent;
            }

            // My pending claims for the open week
            object? myClaims = null;
            if (teamId.HasValue && currentWeek is not null)
            {
                myClaims = currentWeek.WaiverClaims
                    .Where(wc => wc.TeamId == teamId.Value && wc.Status == WaiverClaimStatus.Pending)
                    .OrderBy(wc => wc.Priority)
                    .Select(wc => new
                    {
                        id       = wc.Id,
                        priority = wc.Priority,
                        faabBid  = wc.FaabBid,
                        status   = wc.Status.ToString(),
                        addPlayer = new { wc.AddPlayer.Id, wc.AddPlayer.FirstName, wc.AddPlayer.LastName, wc.AddPlayer.Position, wc.AddPlayer.NflTeam },
                        dropPlayer = wc.DropPlayer is null ? null : new { wc.DropPlayer.Id, wc.DropPlayer.FirstName, wc.DropPlayer.LastName, wc.DropPlayer.Position, wc.DropPlayer.NflTeam } as object,
                    })
                    .ToList();
            }

            // Recent results from last completed week
            object? recentResults = null;
            if (teamId.HasValue)
            {
                var lastCompleted = await db.Weeks
                    .Include(w => w.WaiverClaims).ThenInclude(wc => wc.AddPlayer)
                    .Include(w => w.WaiverClaims).ThenInclude(wc => wc.DropPlayer)
                    .Where(w => w.LeagueId == leagueId && w.Status == WeekStatus.Completed)
                    .OrderByDescending(w => w.WeekNumber)
                    .FirstOrDefaultAsync(ct);

                if (lastCompleted is not null)
                {
                    recentResults = lastCompleted.WaiverClaims
                        .Where(wc => wc.TeamId == teamId.Value)
                        .OrderBy(wc => wc.Priority)
                        .Select(wc => new
                        {
                            id              = wc.Id,
                            priority        = wc.Priority,
                            faabBid         = wc.FaabBid,
                            status          = wc.Status.ToString(),
                            rejectionReason = wc.RejectionReason,
                            weekNumber      = lastCompleted.WeekNumber,
                            addPlayer  = new { wc.AddPlayer.Id, wc.AddPlayer.FirstName, wc.AddPlayer.LastName, wc.AddPlayer.Position, wc.AddPlayer.NflTeam },
                            dropPlayer = wc.DropPlayer is null ? null : new { wc.DropPlayer.Id, wc.DropPlayer.FirstName, wc.DropPlayer.LastName, wc.DropPlayer.Position, wc.DropPlayer.NflTeam } as object,
                        })
                        .ToList();
                }
            }

            // Team roster for drop selection
            List<object>? teamRoster = null;
            if (teamId.HasValue)
            {
                teamRoster = await db.RosterSlots
                    .Where(rs => rs.TeamId == teamId.Value && rs.PlayerId != null)
                    .Include(rs => rs.Player)
                    .OrderBy(rs => rs.IsStarter ? 0 : 1)
                    .Select(rs => (object)new
                    {
                        slotId    = rs.Id,
                        slotType  = rs.SlotType.ToString(),
                        isStarter = rs.IsStarter,
                        player    = rs.Player == null ? null : new
                        {
                            id        = rs.Player.Id,
                            firstName = rs.Player.FirstName,
                            lastName  = rs.Player.LastName,
                            position  = rs.Player.Position,
                            nflTeam   = rs.Player.NflTeam,
                        },
                    })
                    .ToListAsync(ct);
            }

            bool claimsOpen = currentWeek is not null;

            return Results.Ok(new
            {
                weekNumber  = currentWeek?.WeekNumber,
                weekStatus  = currentWeek?.Status.ToString(),
                claimsOpen,
                useFaab     = league.RosterSettings.UseFaab,
                faabBudget  = league.RosterSettings.FaabBudget,
                faabRemaining,
                availablePlayers = new { items = players, totalCount, page, pageSize = PageSize },
                myClaims,
                recentResults,
                teamRoster,
            });
        });

        // POST /api/leagues/{leagueId}/waivers/claims
        // Convenience endpoint — auto-detects the open week and submits the claim.
        group.MapPost("/claims", async (
            Guid leagueId,
            SubmitWaiverRequest req,
            CutlineDbContext db,
            CancellationToken ct) =>
        {
            var week = await db.Weeks
                .Include(w => w.WaiverClaims)
                .Where(w => w.LeagueId == leagueId &&
                            (w.Status == WeekStatus.Eliminated ||
                             w.Status == WeekStatus.InProgress  ||
                             w.Status == WeekStatus.Scoring))
                .OrderByDescending(w => w.WeekNumber)
                .FirstOrDefaultAsync(ct);

            if (week is null)
                return Results.BadRequest(new { error = "No active waiver window." });

            if (week.WaiverClaims.Any(wc => wc.TeamId == req.TeamId &&
                                            wc.AddPlayerId == req.AddPlayerId &&
                                            wc.Status == WaiverClaimStatus.Pending))
                return Results.BadRequest(new { error = "You already have a pending claim for this player." });

            var nextPriority = week.WaiverClaims.Count > 0
                ? week.WaiverClaims.Max(wc => wc.Priority) + 1
                : 1;

            var claim = new WaiverClaim
            {
                Id           = Guid.NewGuid(),
                WeekId       = week.Id,
                TeamId       = req.TeamId,
                AddPlayerId  = req.AddPlayerId,
                DropPlayerId = req.DropPlayerId,
                FaabBid      = req.FaabBid,
                Priority     = nextPriority,
                Status       = WaiverClaimStatus.Pending,
            };

            db.WaiverClaims.Add(claim);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/leagues/{leagueId}/waivers/claims/{claim.Id}",
                new { claimId = claim.Id, weekNumber = week.WeekNumber, priority = claim.Priority });
        });

        // DELETE /api/leagues/{leagueId}/waivers/claims/{claimId}?teamId=
        group.MapDelete("/claims/{claimId:guid}", async (
            Guid leagueId,
            Guid claimId,
            Guid teamId,
            CutlineDbContext db,
            CancellationToken ct) =>
        {
            var claim = await db.WaiverClaims
                .Include(wc => wc.Week)
                .FirstOrDefaultAsync(wc => wc.Id == claimId &&
                                           wc.TeamId == teamId &&
                                           wc.Week.LeagueId == leagueId, ct);

            if (claim is null) return Results.NotFound();
            if (claim.Status != WaiverClaimStatus.Pending)
                return Results.BadRequest(new { error = "Only pending claims can be cancelled." });

            db.WaiverClaims.Remove(claim);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}

public record SubmitWaiverRequest(Guid TeamId, Guid AddPlayerId, Guid? DropPlayerId, decimal? FaabBid);
