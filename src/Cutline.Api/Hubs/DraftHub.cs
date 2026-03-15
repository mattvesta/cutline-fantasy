namespace Cutline.Api.Hubs;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// Pushes live draft updates to connected clients.
/// Clients join/leave a draft room by draftId.
/// Server → client events: PickMade, DraftStarted, DraftCompleted
/// </summary>
public class DraftHub : Hub
{
    public async Task JoinDraft(string draftId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"draft:{draftId}");

    public async Task LeaveDraft(string draftId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"draft:{draftId}");
}
