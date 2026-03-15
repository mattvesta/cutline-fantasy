namespace Cutline.Api.Endpoints;

using Cutline.Core.Interfaces;

public static class PlayersEndpoints
{
    public static RouteGroupBuilder MapPlayers(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/players");

        group.MapGet("/", async (
            string? position,
            string? search,
            string? sortBy,
            bool? sortDesc,
            int page,
            int pageSize,
            IPlayerRepository players,
            CancellationToken ct) =>
        {
            var safePage     = Math.Max(1, page == 0 ? 1 : page);
            var safePageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 10, 100);
            var result = await players.SearchPagedAsync(position, search, sortBy, sortDesc ?? false, safePage, safePageSize, ct);
            return Results.Ok(result);
        });

        group.MapGet("/{playerId:guid}", async (
            Guid playerId,
            IPlayerRepository players,
            CancellationToken ct) =>
        {
            var player = await players.GetByIdAsync(playerId, ct);
            return player is null ? Results.NotFound() : Results.Ok(player);
        });

        return group;
    }
}
