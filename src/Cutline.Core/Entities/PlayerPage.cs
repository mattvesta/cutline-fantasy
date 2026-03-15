namespace Cutline.Core.Entities;

public record PlayerPage(
    IReadOnlyList<Player> Items,
    int TotalCount,
    int Page,
    int PageSize);
