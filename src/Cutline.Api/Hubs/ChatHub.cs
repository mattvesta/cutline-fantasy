namespace Cutline.Api.Hubs;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// Real-time league chat.
/// Clients join/leave the chat group for their league.
/// Server → client event: NewMessage
/// </summary>
public class ChatHub : Hub
{
    public Task JoinLeague(string leagueId)
        => Groups.AddToGroupAsync(Context.ConnectionId, $"chat:{leagueId}");

    public Task LeaveLeague(string leagueId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat:{leagueId}");
}
