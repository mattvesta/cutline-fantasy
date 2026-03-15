namespace Cutline.Core.Interfaces;

using Cutline.Core.Entities;

public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Player?> GetByGsisIdAsync(string gsisId, CancellationToken ct = default);
    Task<Player?> GetBySleeperIdAsync(string sleeperId, CancellationToken ct = default);
    Task<IReadOnlyList<Player>> GetAllAsync(CancellationToken ct = default);
    Task<PlayerPage> SearchPagedAsync(string? position, string? search, string? sortBy, bool sortDesc, int page, int pageSize, CancellationToken ct = default);
    Task UpsertBulkAsync(IEnumerable<Player> players, CancellationToken ct = default);
    Task PatchIdsAsync(IEnumerable<PlayerIdPatch> patches, CancellationToken ct = default);
}
