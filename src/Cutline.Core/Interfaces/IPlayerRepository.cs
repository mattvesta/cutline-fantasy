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

    /// <summary>
    /// For players currently missing a GsisId, attempts to fill it in from nflverse
    /// roster mappings. Tries SleeperId match first (exact), then falls back to
    /// (FirstName + LastName + Position) composite — only accepting unique matches.
    /// Returns the number of players updated.
    /// </summary>
    Task<int> BackfillGsisIdsAsync(IEnumerable<PlayerIdMap> mappings, CancellationToken ct = default);

    /// <summary>
    /// For every GsisId in the nflverse roster that has no corresponding Player row,
    /// creates a minimal Player record so stats can be imported for that player.
    /// Existing players (matched by GsisId) are not modified.
    /// Returns the number of new Player rows created.
    /// </summary>
    Task<int> EnsureNflversePlayersAsync(IEnumerable<PlayerIdMap> mappings, CancellationToken ct = default);
}
