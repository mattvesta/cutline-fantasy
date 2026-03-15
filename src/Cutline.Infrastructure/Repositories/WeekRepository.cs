namespace Cutline.Infrastructure.Repositories;

using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class WeekRepository : IWeekRepository
{
    private readonly CutlineDbContext _db;

    public WeekRepository(CutlineDbContext db) => _db = db;

    public async Task<Week?> GetCurrentWeekAsync(Guid leagueId, CancellationToken ct = default)
        => await _db.Weeks
            .Include(w => w.TeamScores)
            .Where(w => w.LeagueId == leagueId && w.Status != WeekStatus.Completed)
            .OrderBy(w => w.WeekNumber)
            .FirstOrDefaultAsync(ct);

    public async Task<Week?> GetByNumberAsync(Guid leagueId, int weekNumber, CancellationToken ct = default)
        => await _db.Weeks
            .Include(w => w.TeamScores)
            .Include(w => w.WaiverClaims)
            .FirstOrDefaultAsync(w => w.LeagueId == leagueId && w.WeekNumber == weekNumber, ct);

    public async Task<IReadOnlyList<Week>> GetAllAsync(Guid leagueId, CancellationToken ct = default)
        => await _db.Weeks
            .Where(w => w.LeagueId == leagueId)
            .OrderBy(w => w.WeekNumber)
            .ToListAsync(ct);

    public async Task AddAsync(Week week, CancellationToken ct = default)
        => await _db.Weeks.AddAsync(week, ct);

    public async Task AddClaimAsync(WaiverClaim claim, CancellationToken ct = default)
        => await _db.WaiverClaims.AddAsync(claim, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
