namespace Cutline.Core.Interfaces;

using Cutline.Core.Entities;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(Guid leagueId, Guid teamId, CancellationToken ct = default);
    Task<IReadOnlyList<Team>> GetByLeagueAsync(Guid leagueId, CancellationToken ct = default);
    Task AddAsync(Team team, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
