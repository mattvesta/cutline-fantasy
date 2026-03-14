namespace Cutline.AI.Reports;

/// <summary>
/// Hosted platform only — not available in self-hosted instances.
/// Generates personalized elimination risk reports for each team using the Anthropic Claude API.
/// </summary>
public class WeeklyReportService
{
    public async Task<string> GenerateReportAsync(Guid leagueId, Guid teamId, int week, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
