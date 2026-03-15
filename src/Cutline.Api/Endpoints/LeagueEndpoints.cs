namespace Cutline.Api.Endpoints;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;

public static class LeagueEndpoints
{
    public static RouteGroupBuilder MapLeagues(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leagues");

        group.MapGet("/", async (ILeagueRepository leagues, CancellationToken ct) =>
        {
            var all = await leagues.GetAllAsync(ct);
            return Results.Ok(all);
        });

        group.MapGet("/{leagueId:guid}", async (Guid leagueId, ILeagueRepository leagues, CancellationToken ct) =>
        {
            var league = await leagues.GetByIdAsync(leagueId, ct);
            return league is null ? Results.NotFound() : Results.Ok(league);
        });

        group.MapPost("/", async (CreateLeagueRequest req, ILeagueRepository leagues, CancellationToken ct) =>
        {
            var league = new League
            {
                Id     = Guid.NewGuid(),
                Name   = req.Name,
                Season = req.Season,
                Status = LeagueStatus.Setup,
                ScoringSettings = new ScoringSettings
                {
                    ReceptionPoints = req.ReceptionPoints ?? 1m,
                },
                RosterSettings = new RosterSettings
                {
                    QbSlots    = req.QbSlots    ?? 1,
                    RbSlots    = req.RbSlots    ?? 2,
                    WrSlots    = req.WrSlots    ?? 2,
                    TeSlots    = req.TeSlots    ?? 1,
                    FlexSlots  = req.FlexSlots  ?? 1,
                    SuperFlexSlots = req.SuperFlexSlots ?? 0,
                    KSlots     = req.KSlots     ?? 1,
                    DefSlots   = req.DefSlots   ?? 1,
                    BenchSlots = req.BenchSlots ?? 6,
                    IrSlots    = req.IrSlots    ?? 1,
                    UseFaab    = req.UseFaab    ?? false,
                    FaabBudget = req.FaabBudget ?? 100m,
                },
            };

            await leagues.AddAsync(league, ct);
            await leagues.SaveChangesAsync(ct);

            return Results.Created($"/api/leagues/{league.Id}", league);
        });

        return group;
    }
}

public record CreateLeagueRequest(
    string Name,
    int Season,
    // Scoring
    decimal? ReceptionPoints,
    // Roster slots
    int? QbSlots, int? RbSlots, int? WrSlots, int? TeSlots,
    int? FlexSlots, int? SuperFlexSlots, int? KSlots, int? DefSlots,
    int? BenchSlots, int? IrSlots,
    // Waivers
    bool? UseFaab,
    decimal? FaabBudget
);
