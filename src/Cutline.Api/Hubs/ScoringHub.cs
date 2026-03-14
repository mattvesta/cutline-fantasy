namespace Cutline.Api.Hubs;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// Pushes live scoring updates to connected clients.
/// Scoring data flows: ESPN poll → Redis → ScoringHub → clients.
/// </summary>
public class ScoringHub : Hub
{
    public async Task JoinLeague(string leagueId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"league:{leagueId}");

    public async Task LeaveLeague(string leagueId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"league:{leagueId}");
}
