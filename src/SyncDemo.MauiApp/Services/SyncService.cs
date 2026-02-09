using System.Net.Http.Json;
using SyncDemo.MauiApp.Data;
using SyncDemo.MauiApp.Models;
using SyncDemo.Shared.Models;

namespace SyncDemo.MauiApp.Services;

public interface ISyncService
{
    Task<SyncResult> SyncWithServerAsync();
    Task<bool> CreateItemAsync(RealmSyncItem item);
    Task<bool> UpdateItemAsync(RealmSyncItem item);
    Task<bool> DeleteItemAsync(string id);
}

public class SyncService : ISyncService
{
    private readonly IRealmService _realmService;
    private readonly ISignalRService _signalRService;
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl = "http://localhost:5000/api";

    public SyncService(IRealmService realmService, ISignalRService signalRService)
    {
        _realmService = realmService;
        _signalRService = signalRService;
        _httpClient = new HttpClient { BaseAddress = new Uri(_apiBaseUrl) };
    }

    public async Task<SyncResult> SyncWithServerAsync()
    {
        try
        {
            // Get the last sync time (simplified - in production, store this)
            var lastSyncTime = DateTime.UtcNow.AddDays(-30);

            // Fetch updates from server
            var response = await _httpClient.GetAsync($"/syncitems/sync?since={lastSyncTime:O}");
            
            if (!response.IsSuccessStatusCode)
            {
                return new SyncResult
                {
                    Success = false,
                    Message = "Failed to sync with server"
                };
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
            }

            return syncResult ?? new SyncResult { Success = true, Message = "Sync completed" };
        }
        catch (Exception ex)
        {
            return new SyncResult
            {
                Success = false,
                Message = $"Sync error: {ex.Message}"
            };
        }
    }

    public async Task<bool> CreateItemAsync(RealmSyncItem item)
    {
        try
        {
            // Save locally first
            await _realmService.AddOrUpdateItemAsync(item);

            // Send to server
            var syncItem = MapToSyncItem(item);
            var response = await _httpClient.PostAsJsonAsync("/syncitems", syncItem);

            if (response.IsSuccessStatusCode)
            {
                // Broadcast via SignalR
                var message = new SyncMessage
                {
                    Operation = "CREATE",
                    Item = syncItem,
                    Timestamp = DateTime.UtcNow
                };
                await _signalRService.SendSyncUpdateAsync(message);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Create error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateItemAsync(RealmSyncItem item)
    {
        try
        {
            // Save locally first
            await _realmService.AddOrUpdateItemAsync(item);

            // Send to server
            var syncItem = MapToSyncItem(item);
            var response = await _httpClient.PutAsJsonAsync($"/syncitems/{item.Id}", syncItem);

            if (response.IsSuccessStatusCode)
            {
                // Broadcast via SignalR
                var message = new SyncMessage
                {
                    Operation = "UPDATE",
                    Item = syncItem,
                    Timestamp = DateTime.UtcNow
                };
                await _signalRService.SendSyncUpdateAsync(message);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteItemAsync(string id)
    {
        try
        {
            // Delete locally first
            await _realmService.DeleteItemAsync(id);

            // Send to server
            var response = await _httpClient.DeleteAsync($"/syncitems/{id}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Delete error: {ex.Message}");
            return false;
        }
    }

    private SyncItem MapToSyncItem(RealmSyncItem realmItem)
    {
        return new SyncItem
        {
            Id = Guid.Parse(realmItem.Id),
            Name = realmItem.Name,
            Description = realmItem.Description,
            CreatedAt = realmItem.CreatedAt.DateTime,
            ModifiedAt = realmItem.ModifiedAt.DateTime,
            IsDeleted = realmItem.IsDeleted,
            Version = realmItem.Version
        };
    }
}
