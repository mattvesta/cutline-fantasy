namespace Cutline.Core.Interfaces;

using Cutline.Core.Entities;

public interface ILeagueRepository
{
    Task<League?> GetByIdAsync(Guid leagueId, CancellationToken ct = default);
    Task<IReadOnlyList<League>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(League league, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
