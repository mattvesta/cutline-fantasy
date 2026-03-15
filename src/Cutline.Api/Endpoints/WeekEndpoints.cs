namespace Cutline.Api.Endpoints;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;

public static class WeekEndpoints
{
    public static RouteGroupBuilder MapWeeks(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leagues/{leagueId:guid}/weeks");

        group.MapGet("/", async (Guid leagueId, IWeekRepository weeks, CancellationToken ct) =>
        {
            var all = await weeks.GetAllAsync(leagueId, ct);
            return Results.Ok(all);
        });

        group.MapGet("/{weekNumber:int}", async (
            Guid leagueId, int weekNumber, IWeekRepository weeks, CancellationToken ct) =>
        {
            var week = await weeks.GetByNumberAsync(leagueId, weekNumber, ct);
            return week is null ? Results.NotFound() : Results.Ok(week);
        });

        group.MapPost("/", async (
            Guid leagueId, CreateWeekRequest req,
            ILeagueRepository leagues, IWeekRepository weeks, CancellationToken ct) =>
        {
            var league = await leagues.GetByIdAsync(leagueId, ct);
            if (league is null) return Results.NotFound();

            var week = new Week
            {
                Id = Guid.NewGuid(),
                LeagueId = leagueId,
                WeekNumber = req.WeekNumber,
                Status = WeekStatus.Upcoming,
            };

            await weeks.AddAsync(week, ct);
            await weeks.SaveChangesAsync(ct);
            return Results.Created($"/api/leagues/{leagueId}/weeks/{week.WeekNumber}", week);
        });

        // Manually record a team's score for the week (before elimination can run)
        group.MapPost("/{weekNumber:int}/scores", async (
            Guid leagueId, int weekNumber, RecordScoreRequest req,
            IWeekRepository weeks, CancellationToken ct) =>
        {
            var week = await weeks.GetByNumberAsync(leagueId, weekNumber, ct);
            if (week is null) return Results.NotFound();

            var existing = week.TeamScores.FirstOrDefault(ts => ts.TeamId == req.TeamId);
            if (existing is not null)
            {
                existing.Points = req.Points;
                existing.IsLocked = req.IsLocked;
            }
            else
            {
                week.TeamScores.Add(new TeamScore
                {
                    Id = Guid.NewGuid(),
                    WeekId = week.Id,
                    TeamId = req.TeamId,
                    Points = req.Points,
                    IsLocked = req.IsLocked,
                });
            }

            await weeks.SaveChangesAsync(ct);
            return Results.Ok(week);
        });

        // Run elimination — finds lowest scorer, marks them cut, releases their roster
        group.MapPost("/{weekNumber:int}/eliminate", async (
            Guid leagueId, int weekNumber,
            IGuillotineEngine engine, CancellationToken ct) =>
        {
            try
            {
                var result = await engine.RunEliminationAsync(leagueId, weekNumber, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // Submit a waiver claim for the week
        group.MapPost("/{weekNumber:int}/claims", async (
            Guid leagueId, int weekNumber, SubmitClaimRequest req,
            IWeekRepository weeks, CancellationToken ct) =>
        {
            var week = await weeks.GetByNumberAsync(leagueId, weekNumber, ct);
            if (week is null) return Results.NotFound();

            if (week.Status is not (WeekStatus.Eliminated or WeekStatus.Scoring or WeekStatus.InProgress))
                return Results.BadRequest(new { error = "Claims can only be submitted once a team has been eliminated." });

            var nextPriority = week.WaiverClaims.Count > 0
                ? week.WaiverClaims.Max(wc => wc.Priority) + 1
                : 1;

            var claim = new WaiverClaim
            {
                Id = Guid.NewGuid(),
                WeekId = week.Id,
                TeamId = req.TeamId,
                AddPlayerId = req.AddPlayerId,
                DropPlayerId = req.DropPlayerId,
                FaabBid = req.FaabBid,
                Priority = nextPriority,
                Status = WaiverClaimStatus.Pending,
            };

            await weeks.AddClaimAsync(claim, ct);
            await weeks.SaveChangesAsync(ct);
            return Results.Created($"/api/leagues/{leagueId}/weeks/{weekNumber}/claims/{claim.Id}", claim);
        });

        // Process all pending waiver claims for the week
        group.MapPost("/{weekNumber:int}/process-waivers", async (
            Guid leagueId, int weekNumber,
            IWaiverProcessor processor, CancellationToken ct) =>
        {
            try
            {
                var result = await processor.ProcessClaimsAsync(leagueId, weekNumber, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        return group;
    }
}

public record CreateWeekRequest(int WeekNumber);
public record RecordScoreRequest(Guid TeamId, decimal Points, bool IsLocked);
public record SubmitClaimRequest(Guid TeamId, Guid AddPlayerId, Guid? DropPlayerId, decimal? FaabBid);
