namespace Cutline.AI.Trade;

/// <summary>
/// Hosted platform only.
/// Grades trades with survival-aware context — understands you're playing to avoid elimination.
/// </summary>
public class TradeAnalyzerService
{
    public async Task<TradeGrade> AnalyzeAsync(Guid leagueId, Guid tradeId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}

public record TradeGrade(string TeamAGrade, string TeamBGrade, string Analysis);
