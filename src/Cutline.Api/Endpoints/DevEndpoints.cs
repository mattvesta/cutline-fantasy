namespace Cutline.Api.Endpoints;

using Cutline.Core.Entities;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public static class DevEndpoints
{
    public static void MapDevEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dev");

        group.MapPost("/seed", async (CutlineDbContext db, CancellationToken ct) =>
        {
            if (db.Leagues.Any())
                return Results.Ok(new { message = "Already seeded." });

            // Pull top-ADP players by position
            var qbs  = await db.Players.Where(p => p.Position == "QB"  && p.Adp != null).OrderBy(p => p.Adp).Take(16).ToListAsync(ct);
            var rbs  = await db.Players.Where(p => p.Position == "RB"  && p.Adp != null).OrderBy(p => p.Adp).Take(32).ToListAsync(ct);
            var wrs  = await db.Players.Where(p => p.Position == "WR"  && p.Adp != null).OrderBy(p => p.Adp).Take(32).ToListAsync(ct);
            var tes  = await db.Players.Where(p => p.Position == "TE"  && p.Adp != null).OrderBy(p => p.Adp).Take(16).ToListAsync(ct);
            var ks   = await db.Players.Where(p => p.Position == "K"   && p.Adp != null).OrderBy(p => p.Adp).Take(8).ToListAsync(ct);
            var defs = await db.Players.Where(p => p.Position == "DEF" && p.Adp != null).OrderBy(p => p.Adp).Take(8).ToListAsync(ct);

            if (qbs.Count < 8 || rbs.Count < 16 || wrs.Count < 16 || tes.Count < 8)
                return Results.BadRequest(new { message = "Not enough players in DB. Run Sleeper sync first." });

            var league = new League
            {
                Id     = Guid.NewGuid(),
                Name   = "Cutline Test League 2025",
                Season = 2025,
                Status = LeagueStatus.Active,
            };

            var teamNames = new[]
            {
                "The Blade Falls", "Floor Gang", "Waiver Wire Warriors",
                "Survive and Advance", "Chalk Dust", "Last Man Standing",
                "The Underdogs", "Scoreboard Watch",
            };

            foreach (var (name, i) in teamNames.Select((n, i) => (n, i)))
            {
                var team = new Team
                {
                    Id          = Guid.NewGuid(),
                    LeagueId    = league.Id,
                    Name        = name,
                    OwnerUserId = $"dev-user-{i + 1}",
                };

                // Starters (round-robin draft order per position)
                team.RosterSlots.Add(Slot(team.Id, qbs[i],         SlotType.QB,  isStarter: true));
                team.RosterSlots.Add(Slot(team.Id, rbs[i * 2],     SlotType.RB,  isStarter: true));
                team.RosterSlots.Add(Slot(team.Id, rbs[i * 2 + 1], SlotType.RB,  isStarter: true));
                team.RosterSlots.Add(Slot(team.Id, wrs[i * 2],     SlotType.WR,  isStarter: true));
                team.RosterSlots.Add(Slot(team.Id, wrs[i * 2 + 1], SlotType.WR,  isStarter: true));
                team.RosterSlots.Add(Slot(team.Id, tes[i],         SlotType.TE,  isStarter: true));
                team.RosterSlots.Add(Slot(team.Id, ks.Count  > i ? ks[i]   : rbs[i * 2], SlotType.K,   isStarter: true));
                team.RosterSlots.Add(Slot(team.Id, defs.Count > i ? defs[i] : wrs[i * 2], SlotType.DEF, isStarter: true));

                // Bench (6 spots — extra players from RB/WR/TE/QB depth)
                team.RosterSlots.Add(Slot(team.Id, rbs.Count  > 16 + i ? rbs[16 + i]  : rbs[i],  SlotType.Bench, isStarter: false));
                team.RosterSlots.Add(Slot(team.Id, wrs.Count  > 16 + i ? wrs[16 + i]  : wrs[i],  SlotType.Bench, isStarter: false));
                team.RosterSlots.Add(Slot(team.Id, tes.Count  > 8  + i ? tes[8  + i]  : tes[i],  SlotType.Bench, isStarter: false));
                team.RosterSlots.Add(Slot(team.Id, qbs.Count  > 8  + i ? qbs[8  + i]  : qbs[i],  SlotType.Bench, isStarter: false));
                team.RosterSlots.Add(Slot(team.Id, rbs.Count  > 24 + i ? rbs[24 + i]  : rbs[i * 2], SlotType.Bench, isStarter: false));
                team.RosterSlots.Add(Slot(team.Id, wrs.Count  > 24 + i ? wrs[24 + i]  : wrs[i * 2], SlotType.Bench, isStarter: false));

                league.Teams.Add(team);
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

    private static RosterSlot Slot(Guid teamId, Player player, SlotType slotType, bool isStarter) => new()
    {
        Id        = Guid.NewGuid(),
        TeamId    = teamId,
        PlayerId  = player.Id,
        SlotType  = slotType,
        IsStarter = isStarter,
    };
}
