namespace Cutline.Infrastructure.Repositories;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class LeagueRepository : ILeagueRepository
{
    private readonly CutlineDbContext _db;

    public LeagueRepository(CutlineDbContext db) => _db = db;

    public async Task<League?> GetByIdAsync(Guid leagueId, CancellationToken ct = default)
        => await _db.Leagues
            .Include(l => l.Teams).ThenInclude(t => t.Manager)
            .Include(l => l.LeagueManagers).ThenInclude(lm => lm.Manager)
            .FirstOrDefaultAsync(l => l.Id == leagueId, ct);

    public async Task<IReadOnlyList<League>> GetAllAsync(CancellationToken ct = default)
        => await _db.Leagues.ToListAsync(ct);

    public async Task AddAsync(League league, CancellationToken ct = default)
        => await _db.Leagues.AddAsync(league, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
