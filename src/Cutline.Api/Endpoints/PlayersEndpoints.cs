namespace Cutline.Api.Endpoints;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

        // GET /api/players/{playerId}/stats
        // Season totals for every year the player has recorded Final game stats.
        group.MapGet("/{playerId:guid}/stats", async (
            Guid playerId,
            CutlineDbContext db,
            CancellationToken ct) =>
        {
            var rows = await db.PlayerGameStats
                .Where(s => s.PlayerId == playerId && s.GameStatus == GameStatus.Final)
                .GroupBy(s => s.Season)
                .Select(g => new
                {
                    Season                 = g.Key,
                    GamesPlayed            = g.Count(),
                    FantasyPoints          = g.Sum(s => s.Points),
                    PassingYards           = g.Sum(s => s.PassingYards      ?? 0),
                    PassingTDs             = g.Sum(s => s.PassingTDs         ?? 0),
                    Interceptions          = g.Sum(s => s.Interceptions      ?? 0),
                    PassingAttempts        = g.Sum(s => s.PassingAttempts    ?? 0),
                    PassingCompletions     = g.Sum(s => s.PassingCompletions ?? 0),
                    RushingYards           = g.Sum(s => s.RushingYards       ?? 0),
                    RushingTDs             = g.Sum(s => s.RushingTDs         ?? 0),
                    RushingAttempts        = g.Sum(s => s.RushingAttempts    ?? 0),
                    Fumbles                = g.Sum(s => s.Fumbles            ?? 0),
                    Receptions             = g.Sum(s => s.Receptions         ?? 0),
                    Targets                = g.Sum(s => s.Targets            ?? 0),
                    ReceivingYards         = g.Sum(s => s.ReceivingYards     ?? 0),
                    ReceivingTDs           = g.Sum(s => s.ReceivingTDs       ?? 0),
                    FieldGoalsMade         = g.Sum(s => s.FieldGoalsMade         ?? 0),
                    FieldGoalsAttempted    = g.Sum(s => s.FieldGoalsAttempted    ?? 0),
                    ExtraPointsMade        = g.Sum(s => s.ExtraPointsMade        ?? 0),
                    ExtraPointsAttempted   = g.Sum(s => s.ExtraPointsAttempted   ?? 0),
                    Sacks                  = g.Sum(s => s.Sacks                  ?? 0),
                    DefensiveInterceptions = g.Sum(s => s.DefensiveInterceptions ?? 0),
                    FumblesRecovered       = g.Sum(s => s.FumblesRecovered       ?? 0),
                    DefensiveTDs           = g.Sum(s => s.DefensiveTDs           ?? 0),
                    PointsAllowed          = g.Sum(s => s.PointsAllowed          ?? 0),
                    Safeties               = g.Sum(s => s.Safeties               ?? 0),
                })
                .OrderByDescending(r => r.Season)
                .ToListAsync(ct);

            var stats = rows.Select(r => new PlayerSeasonStats(
                r.Season, r.GamesPlayed, r.FantasyPoints,
                r.PassingYards, r.PassingTDs, r.Interceptions, r.PassingAttempts, r.PassingCompletions,
                r.RushingYards, r.RushingTDs, r.RushingAttempts, r.Fumbles,
                r.Receptions, r.Targets, r.ReceivingYards, r.ReceivingTDs,
                r.FieldGoalsMade, r.FieldGoalsAttempted, r.ExtraPointsMade, r.ExtraPointsAttempted,
                r.Sacks, r.DefensiveInterceptions, r.FumblesRecovered, r.DefensiveTDs,
                r.PointsAllowed, r.Safeties
            )).ToList();

            return Results.Ok(stats);
        });

        return group;
    }
}

public record PlayerSeasonStats(
    int     Season,
    int     GamesPlayed,
    decimal FantasyPoints,
    int     PassingYards,
    int     PassingTDs,
    int     Interceptions,
    int     PassingAttempts,
    int     PassingCompletions,
    int     RushingYards,
    int     RushingTDs,
    int     RushingAttempts,
    int     Fumbles,
    int     Receptions,
    int     Targets,
    int     ReceivingYards,
    int     ReceivingTDs,
    int     FieldGoalsMade,
    int     FieldGoalsAttempted,
    int     ExtraPointsMade,
    int     ExtraPointsAttempted,
    int     Sacks,
    int     DefensiveInterceptions,
    int     FumblesRecovered,
    int     DefensiveTDs,
    int     PointsAllowed,
    int     Safeties
);
