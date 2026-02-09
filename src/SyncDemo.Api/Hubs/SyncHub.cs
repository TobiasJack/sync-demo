using Microsoft.AspNetCore.SignalR;
using SyncDemo.Shared.Models;

namespace SyncDemo.Api.Hubs;

public class SyncHub : Hub
{
    private readonly ILogger<SyncHub> _logger;

    public SyncHub(ILogger<SyncHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendSyncUpdate(SyncMessage message)
    {
        _logger.LogInformation("Broadcasting sync update: {Operation} for item {ItemId}", 
            message.Operation, message.Item.Id);
        
        await Clients.Others.SendAsync("ReceiveSyncUpdate", message);
    }

    public async Task RequestSync(DateTime? lastSyncTime)
    {
        _logger.LogInformation("Client {ConnectionId} requested sync from {LastSyncTime}", 
            Context.ConnectionId, lastSyncTime);
        
        // This will be handled by the controller
        await Task.CompletedTask;
    }
}
