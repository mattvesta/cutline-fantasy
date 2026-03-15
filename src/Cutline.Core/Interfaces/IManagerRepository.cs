namespace Cutline.Core.Interfaces;

using Cutline.Core.Entities;

public interface IManagerRepository
{
    Task<Manager?> GetByIdAsync(Guid managerId, CancellationToken ct = default);
    Task<Manager?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<LeagueManager>> GetLeagueMembershipsAsync(Guid leagueId, CancellationToken ct = default);
    Task<LeagueManager?> GetMembershipAsync(Guid leagueId, Guid managerId, CancellationToken ct = default);
    Task AddAsync(Manager manager, CancellationToken ct = default);
    Task AddMembershipAsync(LeagueManager membership, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
