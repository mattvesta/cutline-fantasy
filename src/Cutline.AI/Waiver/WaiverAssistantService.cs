namespace Cutline.AI.Waiver;

/// <summary>
/// Hosted platform only.
/// Recommends survival-first waiver pickups based on standings position and floor metrics.
/// </summary>
public class WaiverAssistantService
{
    public async Task<IReadOnlyList<WaiverRecommendation>> RecommendAsync(Guid leagueId, Guid teamId, int week, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}

public record WaiverRecommendation(string GsisId, string PlayerName, string Reasoning, int Priority);
