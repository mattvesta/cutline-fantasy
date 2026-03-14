namespace Cutline.Api.Endpoints;

public static class LeagueEndpoints
{
    public static RouteGroupBuilder MapLeagues(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leagues");

        // TODO: GET /api/leagues
        // TODO: POST /api/leagues
        // TODO: GET /api/leagues/{leagueId}

        return group;
    }
}
