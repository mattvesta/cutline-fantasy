namespace Cutline.Core.Interfaces;

using Cutline.Core.Entities;

public interface IDraftService
{
    Task<Draft> CreateAsync(Guid leagueId, CancellationToken ct = default);
    Task<Draft> GetAsync(Guid draftId, CancellationToken ct = default);
    Task<Draft?> GetByLeagueAsync(Guid leagueId, CancellationToken ct = default);
    Task<Draft> StartAsync(Guid draftId, CancellationToken ct = default);
    Task<(DraftPick pick, Draft draft)> MakePickAsync(Guid draftId, Guid teamId, Guid playerId, bool isAutoPick = false, CancellationToken ct = default);
    Task<(DraftPick pick, Draft draft)> AutoPickAsync(Guid draftId, CancellationToken ct = default);
    Task<List<Player>> GetAvailablePlayersAsync(Guid draftId, CancellationToken ct = default);
}
