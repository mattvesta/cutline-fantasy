namespace Cutline.Api.Endpoints;

using Cutline.Api.Hubs;
using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Cutline.Infrastructure.Sports;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public static class DevEndpoints
{
    public static void MapDevEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dev");

        group.MapPost("/seed", async (CutlineDbContext db, IPasswordHasher<Manager> hasher, CancellationToken ct) =>
        {
            if (db.Leagues.Any() || db.Managers.Any())
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

            // Create managers (one per team + the commissioner is manager[0])
            var managerSeeds = new[]
            {
                ("Commissioner Chris", "chris@example.com"),
                ("Floor Gang Fred",    "fred@example.com"),
                ("Waiver Wendy",       "wendy@example.com"),
                ("Survivor Sam",       "sam@example.com"),
                ("Chalk Chuck",        "chuck@example.com"),
                ("Last Man Larry",     "larry@example.com"),
                ("Underdog Uma",       "uma@example.com"),
                ("Scoreboard Steve",   "steve@example.com"),
            };

            var managers = managerSeeds.Select((s, i) =>
            {
                var m = new Manager
                {
                    Id          = Guid.NewGuid(),
                    DisplayName = s.Item1,
                    Email       = s.Item2,
                    CreatedAt   = DateTime.UtcNow,
                };
                m.PasswordHash = hasher.HashPassword(m, "password");
                return m;
            }).ToList();

            await db.Managers.AddRangeAsync(managers, ct);

            var league = new League
            {
                Id     = Guid.NewGuid(),
                Name   = "Cutline Test League 2025",
                Season = 2025,
                Status = LeagueStatus.Active,
                RosterSettings = new RosterSettings
                {
                    UseFaab    = true,
                    FaabBudget = 100m,
                    MinFaabBid = 0m,
                },
            };

            // Create league memberships — first manager is commissioner
            for (var i = 0; i < managers.Count; i++)
            {
                league.LeagueManagers.Add(new LeagueManager
                {
                    LeagueId       = league.Id,
                    ManagerId      = managers[i].Id,
                    IsCommissioner = i == 0,
                    JoinedAt       = DateTime.UtcNow,
                });
            }

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
                    OwnerUserId = managers[i].DisplayName,
                    ManagerId   = managers[i].Id,
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

                // Bench (6 spots)
                team.RosterSlots.Add(Slot(team.Id, rbs.Count  > 16 + i ? rbs[16 + i]  : rbs[i],     SlotType.Bench, isStarter: false));
                team.RosterSlots.Add(Slot(team.Id, wrs.Count  > 16 + i ? wrs[16 + i]  : wrs[i],     SlotType.Bench, isStarter: false));
                team.RosterSlots.Add(Slot(team.Id, tes.Count  > 8  + i ? tes[8  + i]  : tes[i],     SlotType.Bench, isStarter: false));
                team.RosterSlots.Add(Slot(team.Id, qbs.Count  > 8  + i ? qbs[8  + i]  : qbs[i],     SlotType.Bench, isStarter: false));
                team.RosterSlots.Add(Slot(team.Id, rbs.Count  > 24 + i ? rbs[24 + i]  : rbs[i * 2], SlotType.Bench, isStarter: false));
                team.RosterSlots.Add(Slot(team.Id, wrs.Count  > 24 + i ? wrs[24 + i]  : wrs[i * 2], SlotType.Bench, isStarter: false));

                league.Teams.Add(team);
            }

            db.Leagues.Add(league);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { leagueId = league.Id, teams = league.Teams.Count, managers = managers.Count });
        });

        // POST /api/dev/import-player-stats?season=2024
        // Manually re-fetches the nflverse player_stats CSV for a season and upserts all rows.
        // Use this after GsisId backfill has run to pick up players that were previously skipped.
        group.MapPost("/import-player-stats", async (
            int season,
            NflverseClient nflverse,
            NflverseStatsImporter importer,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("DevEndpoints");
            logger.LogInformation("Dev: importing player stats for season {Season}", season);
            var stats = await nflverse.FetchAllSeasonStatsAsync(season, ct);
            logger.LogInformation("Dev: fetched {Count} rows for season {Season}", stats.Count, season);
            await importer.ImportAsync(stats, ct);
            return Results.Ok(new { season, rowsFetched = stats.Count });
        });

        group.MapDelete("/seed", async (CutlineDbContext db, CancellationToken ct) =>
        {
            // DraftPicks → Team is Restrict; delete picks before teams/leagues
            db.DraftPicks.RemoveRange(db.DraftPicks);
            await db.SaveChangesAsync(ct);
            db.Leagues.RemoveRange(db.Leagues);
            db.Managers.RemoveRange(db.Managers);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { message = "Seed data cleared." });
        });

        // ── Draft seed ────────────────────────────────────────────────────────
        // Creates a fresh draft league with 8 empty-roster teams, generates a
        // snake draft, starts it, and auto-makes 2 full rounds of picks so the
        // board has some history to display.
        group.MapPost("/seed-draft", async (CutlineDbContext db, IDraftService draftSvc, CancellationToken ct) =>
        {
            // Pull top-ADP players
            var qbs  = await db.Players.Where(p => p.Position == "QB"  && p.Adp != null).OrderBy(p => p.Adp).Take(16).ToListAsync(ct);
            var rbs  = await db.Players.Where(p => p.Position == "RB"  && p.Adp != null).OrderBy(p => p.Adp).Take(48).ToListAsync(ct);
            var wrs  = await db.Players.Where(p => p.Position == "WR"  && p.Adp != null).OrderBy(p => p.Adp).Take(48).ToListAsync(ct);
            var tes  = await db.Players.Where(p => p.Position == "TE"  && p.Adp != null).OrderBy(p => p.Adp).Take(16).ToListAsync(ct);

            if (qbs.Count < 2 || rbs.Count < 8 || wrs.Count < 8 || tes.Count < 2)
                return Results.BadRequest(new { message = "Not enough players in DB. Run Sleeper sync first." });

            var managerSeeds = new[]
            {
                ("Commissioner Chris", "draft-chris@example.com"),
                ("Floor Gang Fred",    "draft-fred@example.com"),
                ("Waiver Wendy",       "draft-wendy@example.com"),
                ("Survivor Sam",       "draft-sam@example.com"),
                ("Chalk Chuck",        "draft-chuck@example.com"),
                ("Last Man Larry",     "draft-larry@example.com"),
                ("Underdog Uma",       "draft-uma@example.com"),
                ("Scoreboard Steve",   "draft-steve@example.com"),
            };

            // Ensure no duplicate emails
            var existingEmails = await db.Managers.Select(m => m.Email).ToListAsync(ct);
            var newEmails = managerSeeds.Select(s => s.Item2).ToList();
            if (newEmails.Any(e => existingEmails.Contains(e)))
                return Results.Ok(new { message = "Draft seed already exists." });

            var managers = managerSeeds.Select(s => new Manager
            {
                Id          = Guid.NewGuid(),
                DisplayName = s.Item1,
                Email       = s.Item2,
                CreatedAt   = DateTime.UtcNow,
            }).ToList();

            await db.Managers.AddRangeAsync(managers, ct);

            var teamNames = new[]
            {
                "The Blade Falls", "Floor Gang", "Waiver Wire Warriors",
                "Survive and Advance", "Chalk Dust", "Last Man Standing",
                "The Underdogs", "Scoreboard Watch",
            };

            var league = new League
            {
                Id     = Guid.NewGuid(),
                Name   = "Cutline Draft League 2025",
                Season = 2025,
                Status = LeagueStatus.Setup,
            };

            var slotTemplate = new (SlotType Type, bool IsStarter)[]
            {
                (SlotType.QB,    true),
                (SlotType.RB,    true),
                (SlotType.RB,    true),
                (SlotType.WR,    true),
                (SlotType.WR,    true),
                (SlotType.TE,    true),
                (SlotType.Flex,  true),
                (SlotType.K,     true),
                (SlotType.DEF,   true),
                (SlotType.Bench, false),
                (SlotType.Bench, false),
                (SlotType.Bench, false),
                (SlotType.Bench, false),
                (SlotType.Bench, false),
                (SlotType.Bench, false),
            };

            for (var i = 0; i < teamNames.Length; i++)
            {
                league.LeagueManagers.Add(new LeagueManager
                {
                    LeagueId       = league.Id,
                    ManagerId      = managers[i].Id,
                    IsCommissioner = i == 0,
                    JoinedAt       = DateTime.UtcNow,
                });

                var team = new Team
                {
                    Id          = Guid.NewGuid(),
                    LeagueId    = league.Id,
                    Name        = teamNames[i],
                    OwnerUserId = managers[i].DisplayName,
                    ManagerId   = managers[i].Id,
                };

                // All slots start empty — the draft will fill them
                foreach (var (type, isStarter) in slotTemplate)
                {
                    team.RosterSlots.Add(new RosterSlot
                    {
                        Id        = Guid.NewGuid(),
                        TeamId    = team.Id,
                        SlotType  = type,
                        IsStarter = isStarter,
                        PlayerId  = null,
                    });
                }

                league.Teams.Add(team);
            }

            db.Leagues.Add(league);
            await db.SaveChangesAsync(ct);

            // Create and start the draft
            var draft = await draftSvc.CreateAsync(league.Id, ct);
            draft = await draftSvc.StartAsync(draft.Id, ct);

            // Auto-pick 2 full rounds so the board has some history
            int autoPickRounds = 2;
            int autoPicks = autoPickRounds * league.Teams.Count;
            for (int i = 0; i < autoPicks; i++)
            {
                if (draft.Status != DraftStatus.InProgress) break;
                (_, draft) = await draftSvc.AutoPickAsync(draft.Id, ct);
            }

            return Results.Ok(new
            {
                leagueId    = league.Id,
                draftId     = draft.Id,
                status      = draft.Status.ToString(),
                picksMade   = draft.Picks.Count(p => p.PlayerId != null),
                totalPicks  = draft.Picks.Count,
                message     = $"Draft league created. {autoPicks} picks auto-completed (rounds 1-{autoPickRounds})."
            });
        });

        group.MapDelete("/seed-draft", async (CutlineDbContext db, CancellationToken ct) =>
        {
            var emails = new[]
            {
                "draft-chris@example.com", "draft-fred@example.com",   "draft-wendy@example.com",
                "draft-sam@example.com",   "draft-chuck@example.com",  "draft-larry@example.com",
                "draft-uma@example.com",   "draft-steve@example.com",
            };
            var managers = await db.Managers.Where(m => emails.Contains(m.Email)).ToListAsync(ct);
            var managerIds = managers.Select(m => m.Id).ToHashSet();
            var leagues = await db.Leagues
                .Include(l => l.LeagueManagers)
                .Where(l => l.LeagueManagers.Any(lm => managerIds.Contains(lm.ManagerId)))
                .ToListAsync(ct);

            var leagueIds = leagues.Select(l => l.Id).ToHashSet();

            // DraftPicks → Team is Restrict; must delete picks before teams/leagues
            var draftIds = await db.Drafts
                .Where(d => leagueIds.Contains(d.LeagueId))
                .Select(d => d.Id)
                .ToListAsync(ct);

            if (draftIds.Count > 0)
            {
                var picks = await db.DraftPicks.Where(p => draftIds.Contains(p.DraftId)).ToListAsync(ct);
                db.DraftPicks.RemoveRange(picks);
                await db.SaveChangesAsync(ct);
            }

            db.Leagues.RemoveRange(leagues);
            db.Managers.RemoveRange(managers);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { message = "Draft seed data cleared." });
        });

        // ── Live scoring seed ─────────────────────────────────────────────────
        // Creates a Week 8 (InProgress) for the first seeded league and upserts
        // mock PlayerGameStats for every rostered starter, then pushes initial
        // SignalR events so the live view is populated.
        group.MapPost("/seed-scores", async (
            CutlineDbContext db,
            ILiveScoringService scoring,
            IHubContext<ScoringHub> hub,
            CancellationToken ct) =>
        {
            var league = await db.Leagues
                .Include(l => l.Teams).ThenInclude(t => t.RosterSlots).ThenInclude(rs => rs.Player)
                .Include(l => l.Weeks)
                .FirstOrDefaultAsync(l => l.Name == "Cutline Test League 2025", ct);

            if (league is null)
                return Results.BadRequest(new { message = "Run POST /api/dev/seed first." });

            const int weekNumber = 8;

            // Ensure Week 8 exists and is InProgress
            var week = league.Weeks.FirstOrDefault(w => w.WeekNumber == weekNumber);
            if (week is null)
            {
                week = new Week { Id = Guid.NewGuid(), LeagueId = league.Id, WeekNumber = weekNumber, Status = WeekStatus.InProgress };
                db.Weeks.Add(week);
            }
            else
            {
                week.Status = WeekStatus.InProgress;
            }

            var rng = new Random(42); // deterministic seed for repeatability

            var allStats = new List<PlayerGameStats>();

            foreach (var team in league.Teams)
            {
                foreach (var slot in team.RosterSlots.Where(rs => rs.IsStarter && rs.Player is not null))
                {
                    var p    = slot.Player!;
                    var stat = new PlayerGameStats
                    {
                        PlayerId    = p.Id,
                        Season      = league.Season,
                        WeekNumber  = weekNumber,
                        GameStatus  = GameStatus.InProgress,
                        Opponent    = $"vs {PickOpponent(rng)}",
                        LastUpdated = DateTime.UtcNow,
                    };

                    PopulateMockStats(p.Position, stat, rng, partial: true);
                    stat.Points = scoring.CalculatePoints(stat, league.ScoringSettings);
                    allStats.Add(stat);
                }
            }

            await db.SaveChangesAsync(ct); // save Week first
            await scoring.UpsertRangeAsync(allStats, ct);

            // Notify connected clients — send to both the team group and the league group
            // so any manager watching the live view sees leaderboard updates for all teams
            foreach (var team in league.Teams)
            {
                var pts     = await scoring.GetTeamStarterPointsAsync(team.Id, league.Season, weekNumber, ct);
                var payload = new { teamId = team.Id, points = pts };
                await hub.Clients.Group($"team:{team.Id}").SendAsync("TeamScoreUpdate", payload, ct);
                await hub.Clients.Group($"league:{league.Id}").SendAsync("TeamScoreUpdate", payload, ct);
            }

            return Results.Ok(new { leagueId = league.Id, weekNumber, playersSeeded = allStats.Count });
        });

        // POST /api/dev/simulate-score-tick
        // Picks a random live player from any team in the seeded league,
        // increments one of their stats, recalculates points, and pushes
        // PlayerStatUpdate + TeamScoreUpdate via SignalR.
        group.MapPost("/simulate-score-tick", async (
            CutlineDbContext db,
            ILiveScoringService scoring,
            IHubContext<ScoringHub> hub,
            CancellationToken ct) =>
        {
            var league = await db.Leagues
                .Include(l => l.Teams).ThenInclude(t => t.RosterSlots)
                .FirstOrDefaultAsync(l => l.Name == "Cutline Test League 2025", ct);

            if (league is null)
                return Results.BadRequest(new { message = "Run seed + seed-scores first." });

            const int weekNumber = 8;

            var liveStats = await db.PlayerGameStats
                .Include(s => s.Player)
                .Where(s => s.Season == league.Season && s.WeekNumber == weekNumber && s.GameStatus == GameStatus.InProgress)
                .ToListAsync(ct);

            if (liveStats.Count == 0)
                return Results.BadRequest(new { message = "No live stats found. Run seed-scores first." });

            var rng  = new Random();
            var stat = liveStats[rng.Next(liveStats.Count)];

            IncrementStat(stat, rng);
            stat.Points      = scoring.CalculatePoints(stat, league.ScoringSettings);
            stat.LastUpdated = DateTime.UtcNow;

            await scoring.UpsertAsync(stat, ct);

            // Find which team owns this player (via a starter slot)
            var ownerSlot = await db.RosterSlots
                .FirstOrDefaultAsync(rs => rs.PlayerId == stat.PlayerId &&
                                           rs.IsStarter &&
                                           league.Teams.Select(t => t.Id).Contains(rs.TeamId), ct);

            if (ownerSlot is not null)
            {
                var pts = await scoring.GetTeamStarterPointsAsync(ownerSlot.TeamId, league.Season, weekNumber, ct);

                await hub.Clients.Group($"team:{ownerSlot.TeamId}")
                    .SendAsync("PlayerStatUpdate", new
                    {
                        playerId    = stat.PlayerId,
                        playerName  = $"{stat.Player.FirstName} {stat.Player.LastName}",
                        position    = stat.Player.Position,
                        points      = stat.Points,
                        gameStatus  = stat.GameStatus.ToString(),
                        opponent    = stat.Opponent,
                        lastUpdated = stat.LastUpdated,
                        // stat lines
                        passingYards = stat.PassingYards, passingTDs = stat.PassingTDs, interceptions = stat.Interceptions,
                        rushingYards = stat.RushingYards,  rushingTDs  = stat.RushingTDs,
                        receptions   = stat.Receptions,    receivingYards = stat.ReceivingYards, receivingTDs = stat.ReceivingTDs,
                        fieldGoalsMade = stat.FieldGoalsMade, extraPointsMade = stat.ExtraPointsMade,
                        sacks = stat.Sacks, defensiveInts = stat.DefensiveInterceptions,
                        defensiveTDs = stat.DefensiveTDs, pointsAllowed = stat.PointsAllowed,
                    }, ct);

                var scorePayload = new { teamId = ownerSlot.TeamId, points = pts };
                await hub.Clients.Group($"team:{ownerSlot.TeamId}").SendAsync("TeamScoreUpdate", scorePayload, ct);
                await hub.Clients.Group($"league:{league.Id}").SendAsync("TeamScoreUpdate", scorePayload, ct);
            }

            return Results.Ok(new
            {
                playerId   = stat.PlayerId,
                playerName = $"{stat.Player.FirstName} {stat.Player.LastName}",
                position   = stat.Player.Position,
                points     = stat.Points,
            });
        });
    }

    // ── Mock data helpers ────────────────────────────────────────────────────

    private static readonly string[] NflTeams = [
        "KC", "SF", "DAL", "PHI", "BUF", "MIA", "BAL", "CIN",
        "GB", "DET", "LAR", "SEA", "NYG", "NYJ", "NE", "LV",
    ];

    private static string PickOpponent(Random rng) => NflTeams[rng.Next(NflTeams.Length)];

    private static void PopulateMockStats(string position, PlayerGameStats s, Random rng, bool partial)
    {
        // partial = first half of game; scale stats down
        double scale = partial ? rng.NextDouble() * 0.6 + 0.2 : 1.0;

        switch (position)
        {
            case "QB":
                s.PassingAttempts    = (int)(rng.Next(28, 45) * scale);
                s.PassingCompletions = (int)(s.PassingAttempts * (rng.NextDouble() * 0.2 + 0.58));
                s.PassingYards       = (int)(rng.Next(180, 340) * scale);
                s.PassingTDs         = (int)(rng.Next(0, 4) * scale);
                s.Interceptions      = rng.NextDouble() < 0.3 * scale ? 1 : 0;
                s.RushingAttempts    = rng.Next(0, 6);
                s.RushingYards       = s.RushingAttempts * rng.Next(3, 9);
                break;
            case "RB":
                s.RushingAttempts = (int)(rng.Next(8, 22) * scale);
                s.RushingYards    = (int)(rng.Next(30, 110) * scale);
                s.RushingTDs      = rng.NextDouble() < 0.35 * scale ? 1 : 0;
                s.Receptions      = (int)(rng.Next(0, 7) * scale);
                s.Targets         = s.Receptions + rng.Next(0, 3);
                s.ReceivingYards  = s.Receptions * rng.Next(5, 12);
                s.Fumbles         = rng.NextDouble() < 0.05 ? 1 : 0;
                break;
            case "WR":
            case "TE":
                s.Targets         = (int)(rng.Next(3, 12) * scale);
                s.Receptions      = (int)(s.Targets * (rng.NextDouble() * 0.3 + 0.5));
                s.ReceivingYards  = (int)(rng.Next(25, 120) * scale);
                s.ReceivingTDs    = rng.NextDouble() < 0.3 * scale ? 1 : 0;
                break;
            case "K":
                s.FieldGoalsMade      = (int)(rng.Next(0, 3) * scale);
                s.FieldGoalsAttempted = s.FieldGoalsMade + (rng.NextDouble() < 0.25 ? 1 : 0);
                s.LongFieldGoal       = s.FieldGoalsMade > 0 ? rng.Next(22, 56) : (int?)null;
                s.ExtraPointsMade     = (int)(rng.Next(0, 4) * scale);
                s.ExtraPointsAttempted= s.ExtraPointsMade;
                break;
            case "DEF":
                s.Sacks                  = (int)(rng.Next(0, 4) * scale);
                s.DefensiveInterceptions = rng.NextDouble() < 0.4 * scale ? rng.Next(1, 3) : 0;
                s.FumblesRecovered       = rng.NextDouble() < 0.2 * scale ? 1 : 0;
                s.DefensiveTDs           = rng.NextDouble() < 0.1 * scale ? 1 : 0;
                s.PointsAllowed          = (int)(rng.Next(0, 35) * scale);
                s.Safeties               = 0;
                break;
        }
    }

    private static void IncrementStat(PlayerGameStats s, Random rng)
    {
        switch (s.Player.Position)
        {
            case "QB":
                var qbRoll = rng.Next(4);
                if (qbRoll == 0) { s.PassingYards    = (s.PassingYards    ?? 0) + rng.Next(5, 25); s.PassingCompletions = (s.PassingCompletions ?? 0) + 1; s.PassingAttempts = (s.PassingAttempts ?? 0) + 1; }
                else if (qbRoll == 1) { s.PassingTDs = (s.PassingTDs ?? 0) + 1; s.PassingYards = (s.PassingYards ?? 0) + rng.Next(8, 20); }
                else if (qbRoll == 2) { s.RushingYards = (s.RushingYards ?? 0) + rng.Next(3, 14); }
                else { s.Interceptions = (s.Interceptions ?? 0) + 1; }
                break;
            case "RB":
                if (rng.Next(3) == 0) { s.RushingTDs = (s.RushingTDs ?? 0) + 1; s.RushingYards = (s.RushingYards ?? 0) + rng.Next(3, 10); }
                else { s.RushingYards = (s.RushingYards ?? 0) + rng.Next(4, 18); }
                break;
            case "WR":
            case "TE":
                if (rng.Next(4) == 0) { s.ReceivingTDs = (s.ReceivingTDs ?? 0) + 1; s.ReceivingYards = (s.ReceivingYards ?? 0) + rng.Next(8, 20); s.Receptions = (s.Receptions ?? 0) + 1; }
                else { s.ReceivingYards = (s.ReceivingYards ?? 0) + rng.Next(5, 22); s.Receptions = (s.Receptions ?? 0) + 1; }
                s.Targets = (s.Targets ?? 0) + 1;
                break;
            case "K":
                if (rng.Next(2) == 0) { s.FieldGoalsMade = (s.FieldGoalsMade ?? 0) + 1; s.FieldGoalsAttempted = (s.FieldGoalsAttempted ?? 0) + 1; s.LongFieldGoal = Math.Max(s.LongFieldGoal ?? 0, rng.Next(22, 55)); }
                else { s.ExtraPointsMade = (s.ExtraPointsMade ?? 0) + 1; s.ExtraPointsAttempted = (s.ExtraPointsAttempted ?? 0) + 1; }
                break;
            case "DEF":
                var defRoll = rng.Next(4);
                if (defRoll == 0) s.Sacks = (s.Sacks ?? 0) + 1;
                else if (defRoll == 1) s.DefensiveInterceptions = (s.DefensiveInterceptions ?? 0) + 1;
                else if (defRoll == 2) s.PointsAllowed = Math.Max(0, (s.PointsAllowed ?? 0) + rng.Next(0, 8));
                else s.FumblesRecovered = (s.FumblesRecovered ?? 0) + 1;
                break;
        }
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
