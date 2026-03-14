namespace Cutline.Api.Endpoints;

using Cutline.Core.Entities;
using Cutline.Infrastructure.Data;

public static class DevEndpoints
{
    public static void MapDevEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dev");

        group.MapPost("/seed", async (CutlineDbContext db, CancellationToken ct) =>
        {
            if (db.Leagues.Any())
                return Results.Ok(new { message = "Already seeded." });

            var league = new League
            {
                Id = Guid.NewGuid(),
                Name = "Cutline Test League 2025",
                Season = 2025,
                Status = LeagueStatus.Active,
            };

            var teamNames = new[]
            {
                "The Blade Falls", "Floor Gang", "Waiver Wire Warriors",
                "Survive and Advance", "Chalk Dust", "Last Man Standing",
                "The Underdogs", "Scoreboard Watch"
            };

            foreach (var (name, i) in teamNames.Select((n, i) => (n, i)))
            {
                league.Teams.Add(new Team
                {
                    Id = Guid.NewGuid(),
                    LeagueId = league.Id,
                    Name = name,
                    OwnerUserId = $"dev-user-{i + 1}",
                });
            }

            db.Leagues.Add(league);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { leagueId = league.Id, teams = league.Teams.Count });
        });

        group.MapDelete("/seed", async (CutlineDbContext db, CancellationToken ct) =>
        {
            db.Leagues.RemoveRange(db.Leagues);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { message = "Seed data cleared." });
        });
    }
}
