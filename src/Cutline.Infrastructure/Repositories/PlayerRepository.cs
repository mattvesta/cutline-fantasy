namespace Cutline.Infrastructure.Repositories;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class PlayerRepository : IPlayerRepository
{
    private readonly CutlineDbContext _db;

    public PlayerRepository(CutlineDbContext db) => _db = db;

    public async Task<Player?> GetByGsisIdAsync(string gsisId, CancellationToken ct = default)
        => await _db.Players.FirstOrDefaultAsync(p => p.GsisId == gsisId, ct);

    public async Task<Player?> GetBySleeperIdAsync(string sleeperId, CancellationToken ct = default)
        => await _db.Players.FirstOrDefaultAsync(p => p.SleeperId == sleeperId, ct);

    public async Task<IReadOnlyList<Player>> GetAllAsync(CancellationToken ct = default)
        => await _db.Players.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync(ct);

    public async Task UpsertBulkAsync(IEnumerable<Player> players, CancellationToken ct = default)
    {
        var incoming = players.ToList();
        var gsisIds = incoming.Select(p => p.GsisId).ToHashSet();
        var existing = await _db.Players
            .Where(p => gsisIds.Contains(p.GsisId))
            .ToDictionaryAsync(p => p.GsisId, ct);

        foreach (var player in incoming)
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
