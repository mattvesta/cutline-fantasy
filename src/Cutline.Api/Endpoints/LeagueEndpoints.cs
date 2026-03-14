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
                Id = Guid.NewGuid(),
                Name = req.Name,
                Season = req.Season,
                Status = LeagueStatus.Setup,
            };

            await leagues.AddAsync(league, ct);
            await leagues.SaveChangesAsync(ct);

            return Results.Created($"/api/leagues/{league.Id}", league);
        });

        return group;
    }
}

public record CreateLeagueRequest(string Name, int Season);
