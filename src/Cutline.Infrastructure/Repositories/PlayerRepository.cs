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
                .Where(p => gsisIds.Contains(p.GsisId))
                .ToDictionaryAsync(p => p.GsisId, ct);

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
        // Sleeper API can return duplicate gsis_ids — keep the last occurrence of each.
        var incoming = players
            .GroupBy(p => p.GsisId)
            .Select(g => g.Last())
            .ToList();

        // Process in batches to avoid oversized transactions.
        const int batchSize = 500;
        for (var offset = 0; offset < incoming.Count; offset += batchSize)
        {
            var batch = incoming.Skip(offset).Take(batchSize).ToList();
            var gsisIds = batch.Select(p => p.GsisId).ToHashSet();
            var existing = await _db.Players
                .Where(p => gsisIds.Contains(p.GsisId))
                .ToDictionaryAsync(p => p.GsisId, ct);

            foreach (var player in batch)
            {
                if (existing.TryGetValue(player.GsisId, out var current))
                {
                    current.FirstName = player.FirstName;
                    current.LastName = player.LastName;
                    current.Position = player.Position;
                    current.NflTeam = player.NflTeam;
                    current.Status = player.Status;
                    current.ByeWeek = player.ByeWeek;
                    current.SleeperId = player.SleeperId;
                    current.EspnId = player.EspnId;
                    current.Age = player.Age;
                    current.College = player.College;
                    current.Height = player.Height;
                    current.Weight = player.Weight;
                    current.JerseyNumber = player.JerseyNumber;
                    current.YearsExperience = player.YearsExperience;
                    current.DepthChartOrder = player.DepthChartOrder;
                    current.Adp = player.Adp;
                    current.LastSyncedAt = player.LastSyncedAt;
                }
                else
                {
                    player.Id = Guid.NewGuid();
                    await _db.Players.AddAsync(player, ct);
                }
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
