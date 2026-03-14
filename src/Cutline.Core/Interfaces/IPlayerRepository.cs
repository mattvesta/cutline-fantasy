namespace Cutline.Core.Interfaces;

using Cutline.Core.Entities;

public interface IPlayerRepository
{
    Task<Player?> GetByGsisIdAsync(string gsisId, CancellationToken ct = default);
    Task<Player?> GetBySleeperIdAsync(string sleeperId, CancellationToken ct = default);
    Task<IReadOnlyList<Player>> GetAllAsync(CancellationToken ct = default);
    Task UpsertBulkAsync(IEnumerable<Player> players, CancellationToken ct = default);
}
