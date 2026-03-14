namespace Cutline.Infrastructure.Repositories;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class TeamRepository : ITeamRepository
{
    private readonly CutlineDbContext _db;

    public TeamRepository(CutlineDbContext db) => _db = db;

    public async Task<Team?> GetByIdAsync(Guid leagueId, Guid teamId, CancellationToken ct = default)
        => await _db.Teams
            .Include(t => t.RosterSlots).ThenInclude(rs => rs.Player)
            .FirstOrDefaultAsync(t => t.LeagueId == leagueId && t.Id == teamId, ct);

    public async Task<IReadOnlyList<Team>> GetByLeagueAsync(Guid leagueId, CancellationToken ct = default)
        => await _db.Teams
            .Where(t => t.LeagueId == leagueId)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    public async Task AddAsync(Team team, CancellationToken ct = default)
        => await _db.Teams.AddAsync(team, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
