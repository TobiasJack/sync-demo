using Microsoft.AspNetCore.SignalR.Client;
using SyncDemo.Shared.Models;
using SyncDemo.WpfApp.Models;
using System.Net.Http.Json;

namespace SyncDemo.WpfApp.Services;

public class DataUpdatedEventArgs : EventArgs
{
    public string EntityType { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
}

public interface ISyncService
{
    Task ConnectAsync(string deviceId);
    Task DisconnectAsync();
    bool IsConnected { get; }
    event EventHandler<DataUpdatedEventArgs>? DataUpdated;
}

public class SyncService : ISyncService
{
    private readonly IRealmService _realmService;
    private HubConnection? _hubConnection;
    private readonly HttpClient _httpClient;
    private readonly string _hubUrl = "http://localhost:5000/synchub";
    private readonly string _apiBaseUrl = "http://localhost:5000/api";
    private string _deviceId = string.Empty;

    public event EventHandler<DataUpdatedEventArgs>? DataUpdated;
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public SyncService(IRealmService realmService)
    {
        _realmService = realmService;
        _httpClient = new HttpClient { BaseAddress = new Uri(_apiBaseUrl) };
    }

    public async Task ConnectAsync(string deviceId)
    {
        _deviceId = deviceId;

        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<SyncMessage>("ReceiveSyncUpdate", async (message) =>
        {
            await HandleSyncUpdate(message);
        });

        _hubConnection.Reconnecting += (error) =>
        {
            System.Diagnostics.Debug.WriteLine($"SignalR reconnecting: {error?.Message}");
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += async (connectionId) =>
        {
            System.Diagnostics.Debug.WriteLine($"SignalR reconnected: {connectionId}");
            // Perform sync after reconnect to catch up on missed messages
            await SyncWithServerAsync();
        };

        try
        {
            await _hubConnection.StartAsync();
            
            // Perform initial sync
            await SyncWithServerAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SignalR connection error: {ex.Message}");
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    private async Task SyncWithServerAsync()
    {
        try
        {
            // Get the last sync time (simplified - in production, store this)
            var lastSyncTime = DateTime.UtcNow.AddDays(-30);

            // Fetch updates from server
            var response = await _httpClient.GetAsync($"/syncitems/sync?since={lastSyncTime:O}");
            
            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine("Failed to sync with server");
                return;
            }

            var syncResult = await response.Content.ReadFromJsonAsync<SyncResult>();
            
            if (syncResult?.Items != null)
            {
                foreach (var item in syncResult.Items)
                {
                    var realmItem = new RealmSyncItem
                    {
                        Id = item.Id.ToString(),
                        Name = item.Name,
                        Description = item.Description,
                        CreatedAt = new DateTimeOffset(item.CreatedAt),
                        ModifiedAt = new DateTimeOffset(item.ModifiedAt),
                        IsDeleted = item.IsDeleted,
                        Version = item.Version
                    };

                    await _realmService.AddOrUpdateItemAsync(realmItem);
                }

                // Notify UI of updates
                DataUpdated?.Invoke(this, new DataUpdatedEventArgs 
                { 
                    EntityType = "ALL", 
                    Operation = "SYNC" 
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Sync error: {ex.Message}");
        }
    }

    private async Task HandleSyncUpdate(SyncMessage message)
    {
        try
        {
            var realmItem = new RealmSyncItem
            {
                Id = message.Item.Id.ToString(),
                Name = message.Item.Name,
                Description = message.Item.Description,
                CreatedAt = new DateTimeOffset(message.Item.CreatedAt),
                ModifiedAt = new DateTimeOffset(message.Item.ModifiedAt),
                IsDeleted = message.Item.IsDeleted,
                Version = message.Item.Version
            };

            if (message.Operation == "DELETE")
            {
                await _realmService.DeleteItemAsync(realmItem.Id);
            }
            else
            {
                await _realmService.AddOrUpdateItemAsync(realmItem);
            }

            // Notify UI of updates
            DataUpdated?.Invoke(this, new DataUpdatedEventArgs 
            { 
                EntityType = "ITEMS", 
                Operation = message.Operation 
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Handle sync update error: {ex.Message}");
        }
    }
}
