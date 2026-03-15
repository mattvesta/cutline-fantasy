namespace Cutline.Core.Interfaces;

using Cutline.Core.Entities;

public interface IWeekRepository
{
    Task<Week?> GetCurrentWeekAsync(Guid leagueId, CancellationToken ct = default);
    Task<Week?> GetByNumberAsync(Guid leagueId, int weekNumber, CancellationToken ct = default);
    Task<IReadOnlyList<Week>> GetAllAsync(Guid leagueId, CancellationToken ct = default);
    Task AddAsync(Week week, CancellationToken ct = default);
    Task AddClaimAsync(WaiverClaim claim, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
