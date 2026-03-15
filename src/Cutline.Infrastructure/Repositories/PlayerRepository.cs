namespace Cutline.Infrastructure.Repositories;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class PlayerRepository : IPlayerRepository
{
    private readonly CutlineDbContext _db;

    public PlayerRepository(CutlineDbContext db) => _db = db;

    public async Task<Player?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Players.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Player?> GetByGsisIdAsync(string gsisId, CancellationToken ct = default)
        => await _db.Players.FirstOrDefaultAsync(p => p.GsisId == gsisId, ct);

    public async Task<Player?> GetBySleeperIdAsync(string sleeperId, CancellationToken ct = default)
        => await _db.Players.FirstOrDefaultAsync(p => p.SleeperId == sleeperId, ct);

    public async Task<IReadOnlyList<Player>> GetAllAsync(CancellationToken ct = default)
        => await _db.Players.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync(ct);

    public async Task<PlayerPage> SearchPagedAsync(
        string? position, string? search, string? sortBy, bool sortDesc,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Players.AsQueryable();

        if (!string.IsNullOrWhiteSpace(position))
            query = query.Where(p => p.Position == position);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(term) ||
                p.LastName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        query = sortBy?.ToLowerInvariant() switch
        {
            "name"     => sortDesc
                ? query.OrderByDescending(p => p.LastName).ThenByDescending(p => p.FirstName)
                : query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName),
            "position" => sortDesc
                ? query.OrderByDescending(p => p.Position).ThenBy(p => p.LastName)
                : query.OrderBy(p => p.Position).ThenBy(p => p.LastName),
            "team"     => sortDesc
                ? query.OrderByDescending(p => p.NflTeam).ThenBy(p => p.LastName)
                : query.OrderBy(p => p.NflTeam).ThenBy(p => p.LastName),
            "status"   => sortDesc
                ? query.OrderByDescending(p => p.Status).ThenBy(p => p.LastName)
                : query.OrderBy(p => p.Status).ThenBy(p => p.LastName),
            // Default: ADP ascending, nulls last
            _          => sortDesc
                ? query.OrderBy(p => p.Adp == null).ThenByDescending(p => p.Adp).ThenBy(p => p.LastName)
                : query.OrderBy(p => p.Adp == null).ThenBy(p => p.Adp).ThenBy(p => p.LastName),
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PlayerPage(items, totalCount, page, pageSize);
    }

    public async Task PatchIdsAsync(IEnumerable<PlayerIdPatch> patches, CancellationToken ct = default)
    {
        var patchList = patches.ToList();

        const int batchSize = 500;
        for (var offset = 0; offset < patchList.Count; offset += batchSize)
        {
            var batch = patchList.Skip(offset).Take(batchSize).ToList();
            var gsisIds = batch.Select(p => p.GsisId).ToHashSet();
            var existing = await _db.Players
                .Where(p => p.GsisId != null && gsisIds.Contains(p.GsisId!))
                .ToDictionaryAsync(p => p.GsisId!, ct);

            foreach (var patch in batch)
            {
                if (!existing.TryGetValue(patch.GsisId, out var player)) continue;
                if (patch.EspnId is not null) player.EspnId = patch.EspnId;
                if (patch.SleeperId is not null) player.SleeperId = patch.SleeperId;
            }

            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task UpsertBulkAsync(IEnumerable<Player> players, CancellationToken ct = default)
    {
        // Split into two sets: players with a known GsisId (keyed by GsisId) and those without
        // (keyed by SleeperId — always present for Sleeper-sourced records).
        // Deduplicate each set, keeping the last occurrence.
        var withGsis    = players.Where(p => p.GsisId != null)
                                 .GroupBy(p => p.GsisId!)
                                 .Select(g => g.Last())
                                 .ToList();
        var withoutGsis = players.Where(p => p.GsisId == null && p.SleeperId != null)
                                 .GroupBy(p => p.SleeperId!)
                                 .Select(g => g.Last())
                                 .ToList();

        const int batchSize = 500;

        // ── Players with a GsisId ────────────────────────────────────────────
        for (var offset = 0; offset < withGsis.Count; offset += batchSize)
        {
            var batch = withGsis.Skip(offset).Take(batchSize).ToList();
            var gsisIds = batch.Select(p => p.GsisId!).ToHashSet();
            var existing = await _db.Players
                .Where(p => p.GsisId != null && gsisIds.Contains(p.GsisId!))
                .ToDictionaryAsync(p => p.GsisId!, ct);

            foreach (var player in batch)
            {
                if (existing.TryGetValue(player.GsisId!, out var current))
                    CopyFields(player, current);
                else
                {
                    player.Id = Guid.NewGuid();
                    await _db.Players.AddAsync(player, ct);
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        // ── Players without a GsisId (keyed by SleeperId until backfilled) ──
        for (var offset = 0; offset < withoutGsis.Count; offset += batchSize)
        {
            var batch = withoutGsis.Skip(offset).Take(batchSize).ToList();
            var sleeperIds = batch.Select(p => p.SleeperId!).ToHashSet();
            var existing = await _db.Players
                .Where(p => p.SleeperId != null && sleeperIds.Contains(p.SleeperId!))
                .ToDictionaryAsync(p => p.SleeperId!, ct);

            foreach (var player in batch)
            {
                if (existing.TryGetValue(player.SleeperId!, out var current))
                    CopyFields(player, current);
                else
                {
                    player.Id = Guid.NewGuid();
                    await _db.Players.AddAsync(player, ct);
                }
            }

            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<int> BackfillGsisIdsAsync(IEnumerable<PlayerIdMap> mappings, CancellationToken ct = default)
    {
        var mapList = mappings.ToList();

        // ── Build lookup maps from nflverse data ────────────────────────────

        // Primary: sleeper_id → gsis_id (exact, zero ambiguity)
        var bySleeperNfl = mapList
            .Where(m => !string.IsNullOrEmpty(m.SleeperId))
            .ToDictionary(m => m.SleeperId!, m => m.GsisId, StringComparer.Ordinal);

        // Fallback: (firstName·lastName·position) → gsis_id
        // Only keep entries that are unique — duplicates could produce false positives.
        var byNameGroup = mapList
            .Where(m => !string.IsNullOrEmpty(m.FirstName) && !string.IsNullOrEmpty(m.LastName) && !string.IsNullOrEmpty(m.Position))
            .GroupBy(m => NameKey(m.FirstName!, m.LastName!, m.Position!))
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.Single().GsisId, StringComparer.OrdinalIgnoreCase);

        // ── Load all players currently missing a GsisId ──────────────────────
        var unmatched = await _db.Players
            .Where(p => p.GsisId == null)
            .ToListAsync(ct);

        if (unmatched.Count == 0) return 0;

        // Pre-fetch all GsisIds already claimed by other players — avoids N+1 queries.
        var occupiedGsisIds = await _db.Players
            .Where(p => p.GsisId != null)
            .Select(p => p.GsisId!)
            .ToHashSetAsync(ct);

        var updated = 0;

        foreach (var player in unmatched)
        {
            string? gsisId = null;

            // Step 1 — match by SleeperId
            if (player.SleeperId is not null)
                bySleeperNfl.TryGetValue(player.SleeperId, out gsisId);

            // Step 2 — match by name + position composite
            if (gsisId is null
                && !string.IsNullOrEmpty(player.FirstName)
                && !string.IsNullOrEmpty(player.LastName)
                && !string.IsNullOrEmpty(player.Position))
            {
                byNameGroup.TryGetValue(NameKey(player.FirstName, player.LastName, player.Position), out gsisId);
            }

            if (gsisId is null) continue;
            if (occupiedGsisIds.Contains(gsisId)) continue;

            player.GsisId = gsisId;
            occupiedGsisIds.Add(gsisId);   // claim it so no other player in this run can take it
            updated++;
        }

        if (updated > 0)
            await _db.SaveChangesAsync(ct);

        return updated;
    }

    private static string NameKey(string first, string last, string position) =>
        $"{first.Trim().ToLowerInvariant()}|{last.Trim().ToLowerInvariant()}|{position.Trim().ToLowerInvariant()}";

    private static void CopyFields(Player src, Player dst)
    {
        dst.GsisId          = src.GsisId;
        dst.FirstName       = src.FirstName;
        dst.LastName        = src.LastName;
        dst.Position        = src.Position;
        dst.NflTeam         = src.NflTeam;
        dst.Status          = src.Status;
        dst.ByeWeek         = src.ByeWeek;
        dst.SleeperId       = src.SleeperId;
        dst.EspnId          = src.EspnId;
        dst.Age             = src.Age;
        dst.College         = src.College;
        dst.Height          = src.Height;
        dst.Weight          = src.Weight;
        dst.JerseyNumber    = src.JerseyNumber;
        dst.YearsExperience = src.YearsExperience;
        dst.DepthChartOrder = src.DepthChartOrder;
        dst.Adp             = src.Adp;
        dst.LastSyncedAt    = src.LastSyncedAt;
    }
}
