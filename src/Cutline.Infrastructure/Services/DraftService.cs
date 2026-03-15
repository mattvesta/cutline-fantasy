namespace Cutline.Infrastructure.Services;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class DraftService(CutlineDbContext db) : IDraftService
{
    // Roster slot types in the order we should fill them during a draft
    private static readonly List<(SlotType Slot, bool IsStarter)> DefaultSlots =
    [
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
    ];

    public async Task<Draft> CreateAsync(Guid leagueId, CancellationToken ct = default)
    {
        var league = await db.Leagues
            .Include(l => l.Teams)
            .FirstOrDefaultAsync(l => l.Id == leagueId, ct)
            ?? throw new InvalidOperationException("League not found");

        if (await db.Drafts.AnyAsync(d => d.LeagueId == leagueId, ct))
            throw new InvalidOperationException("Draft already exists for this league");

        var teams = league.Teams.OrderBy(t => t.Name).ToList();
        if (teams.Count == 0)
            throw new InvalidOperationException("League has no teams");

        int rounds = DefaultSlots.Count;
        var picks = GenerateSnakePicks(teams.Select(t => t.Id).ToList(), rounds);

        var draft = new Draft
        {
            Id                 = Guid.NewGuid(),
            LeagueId           = leagueId,
            Status             = DraftStatus.Pending,
            PickTimeLimitSeconds = 90,
            CurrentPickNumber  = 1,
            Picks              = picks,
        };

        league.Status = LeagueStatus.Drafting;

        db.Drafts.Add(draft);
        await db.SaveChangesAsync(ct);
        return await GetAsync(draft.Id, ct);
    }

    public async Task<Draft> GetAsync(Guid draftId, CancellationToken ct = default)
    {
        return await db.Drafts
            .Include(d => d.Picks.OrderBy(p => p.PickNumber))
                .ThenInclude(p => p.Team)
            .Include(d => d.Picks)
                .ThenInclude(p => p.Player)
            .FirstOrDefaultAsync(d => d.Id == draftId, ct)
            ?? throw new InvalidOperationException("Draft not found");
    }

    public async Task<Draft?> GetByLeagueAsync(Guid leagueId, CancellationToken ct = default)
    {
        var draftId = await db.Drafts
            .Where(d => d.LeagueId == leagueId)
            .Select(d => d.Id)
            .FirstOrDefaultAsync(ct);

        return draftId == Guid.Empty ? null : await GetAsync(draftId, ct);
    }

    public async Task<Draft> StartAsync(Guid draftId, CancellationToken ct = default)
    {
        var draft = await db.Drafts.FindAsync([draftId], ct)
            ?? throw new InvalidOperationException("Draft not found");

        if (draft.Status != DraftStatus.Pending)
            throw new InvalidOperationException("Draft has already started");

        draft.Status               = DraftStatus.InProgress;
        draft.StartedAt            = DateTime.UtcNow;
        draft.CurrentPickStartedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetAsync(draftId, ct);
    }

    public async Task<(DraftPick pick, Draft draft)> MakePickAsync(
        Guid draftId, Guid teamId, Guid playerId,
        bool isAutoPick = false, CancellationToken ct = default)
    {
        var draft = await db.Drafts
            .Include(d => d.Picks)
            .FirstOrDefaultAsync(d => d.Id == draftId, ct)
            ?? throw new InvalidOperationException("Draft not found");

        if (draft.Status != DraftStatus.InProgress)
            throw new InvalidOperationException("Draft is not in progress");

        var currentPick = draft.Picks.FirstOrDefault(p => p.PickNumber == draft.CurrentPickNumber)
            ?? throw new InvalidOperationException("No current pick found");

        if (currentPick.TeamId != teamId)
            throw new InvalidOperationException($"It is not this team's turn to pick");

        if (draft.Picks.Any(p => p.PlayerId == playerId))
            throw new InvalidOperationException("Player has already been drafted");

        currentPick.PlayerId   = playerId;
        currentPick.PickedAt   = DateTime.UtcNow;
        currentPick.IsAutoPick = isAutoPick;

        // Assign player to the team's next available roster slot
        await AssignToRosterAsync(teamId, playerId, ct);

        // Advance draft
        int totalPicks = draft.Picks.Count;
        draft.CurrentPickNumber++;
        draft.CurrentPickStartedAt = DateTime.UtcNow;

        if (draft.CurrentPickNumber > totalPicks)
        {
            draft.Status       = DraftStatus.Completed;
            draft.CompletedAt  = DateTime.UtcNow;
            var league = await db.Leagues.FindAsync([draft.LeagueId], ct);
            if (league != null) league.Status = LeagueStatus.Active;
        }

        await db.SaveChangesAsync(ct);

        var updatedDraft = await GetAsync(draftId, ct);
        var updatedPick  = updatedDraft.Picks.First(p => p.PickedAt == currentPick.PickedAt && p.PlayerId == playerId);
        return (updatedPick, updatedDraft);
    }

    public async Task<(DraftPick pick, Draft draft)> AutoPickAsync(Guid draftId, CancellationToken ct = default)
    {
        var draft = await db.Drafts
            .Include(d => d.Picks)
            .FirstOrDefaultAsync(d => d.Id == draftId, ct)
            ?? throw new InvalidOperationException("Draft not found");

        if (draft.Status != DraftStatus.InProgress)
            throw new InvalidOperationException("Draft is not in progress");

        var currentPick = draft.Picks.FirstOrDefault(p => p.PickNumber == draft.CurrentPickNumber)
            ?? throw new InvalidOperationException("No current pick found");

        var pickedIds = draft.Picks
            .Where(p => p.PlayerId.HasValue)
            .Select(p => p.PlayerId!.Value)
            .ToHashSet();

        var bestPlayer = await db.Players
            .Where(p => p.Adp != null && !pickedIds.Contains(p.Id))
            .OrderBy(p => p.Adp)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No available players to auto-pick");

        return await MakePickAsync(draftId, currentPick.TeamId, bestPlayer.Id, isAutoPick: true, ct);
    }

    public async Task<List<Player>> GetAvailablePlayersAsync(Guid draftId, CancellationToken ct = default)
    {
        var pickedIds = await db.DraftPicks
            .Where(p => p.DraftId == draftId && p.PlayerId != null)
            .Select(p => p.PlayerId!.Value)
            .ToListAsync(ct);

        return await db.Players
            .Where(p => !pickedIds.Contains(p.Id) && p.Adp != null)
            .OrderBy(p => p.Adp)
            .ToListAsync(ct);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static List<DraftPick> GenerateSnakePicks(List<Guid> teamIds, int rounds)
    {
        var picks = new List<DraftPick>();
        int overall = 1;

        for (int round = 1; round <= rounds; round++)
        {
            var order = round % 2 == 1
                ? teamIds
                : Enumerable.Reverse(teamIds).ToList();

            for (int i = 0; i < order.Count; i++)
            {
                picks.Add(new DraftPick
                {
                    Id         = Guid.NewGuid(),
                    TeamId     = order[i],
                    PickNumber = overall++,
                    Round      = round,
                    RoundPick  = i + 1,
                });
            }
        }

        return picks;
    }

    private async Task AssignToRosterAsync(Guid teamId, Guid playerId, CancellationToken ct)
    {
        var player = await db.Players.FindAsync([playerId], ct)
            ?? throw new InvalidOperationException("Player not found");

        var emptySlots = await db.RosterSlots
            .Where(s => s.TeamId == teamId && s.PlayerId == null)
            .ToListAsync(ct);

        var slot = FindBestSlot(emptySlots, player.Position);
        if (slot != null)
            slot.PlayerId = playerId;
        // If no slot found (team full), we silently skip — pick is still recorded
    }

    private static RosterSlot? FindBestSlot(List<RosterSlot> emptySlots, string position)
    {
        var candidates = new List<SlotType>();

        // Position-specific slot first
        if (Enum.TryParse<SlotType>(position, out var exact))
            candidates.Add(exact);

        // Flex eligibility
        if (position is "RB" or "WR" or "TE")  candidates.Add(SlotType.Flex);
        if (position is "QB" or "RB" or "WR" or "TE") candidates.Add(SlotType.SuperFlex);

        // Bench is always valid
        candidates.Add(SlotType.Bench);

        foreach (var slotType in candidates)
        {
            var slot = emptySlots.FirstOrDefault(s => s.SlotType == slotType);
            if (slot != null) return slot;
        }

        return null;
    }
}
