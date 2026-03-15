namespace Cutline.Infrastructure.Repositories;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class ManagerRepository : IManagerRepository
{
    private readonly CutlineDbContext _db;

    public ManagerRepository(CutlineDbContext db) => _db = db;

    public async Task<Manager?> GetByIdAsync(Guid managerId, CancellationToken ct = default)
        => await _db.Managers
            .Include(m => m.LeagueManagers).ThenInclude(lm => lm.League)
            .Include(m => m.Teams)
            .FirstOrDefaultAsync(m => m.Id == managerId, ct);

    public async Task<Manager?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _db.Managers.FirstOrDefaultAsync(m => m.Email == email.ToLower(), ct);

    public async Task<IReadOnlyList<LeagueManager>> GetLeagueMembershipsAsync(Guid leagueId, CancellationToken ct = default)
        => await _db.LeagueManagers
            .Where(lm => lm.LeagueId == leagueId)
            .Include(lm => lm.Manager)
            .Include(lm => lm.Manager).ThenInclude(m => m.Teams.Where(t => t.LeagueId == leagueId))
            .OrderByDescending(lm => lm.IsCommissioner)
            .ThenBy(lm => lm.Manager.DisplayName)
            .ToListAsync(ct);

    public async Task<LeagueManager?> GetMembershipAsync(Guid leagueId, Guid managerId, CancellationToken ct = default)
        => await _db.LeagueManagers
            .Include(lm => lm.Manager)
            .FirstOrDefaultAsync(lm => lm.LeagueId == leagueId && lm.ManagerId == managerId, ct);

    public async Task AddAsync(Manager manager, CancellationToken ct = default)
        => await _db.Managers.AddAsync(manager, ct);

    public async Task AddMembershipAsync(LeagueManager membership, CancellationToken ct = default)
        => await _db.LeagueManagers.AddAsync(membership, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
