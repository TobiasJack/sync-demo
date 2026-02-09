using System.Net.Http;
using Microsoft.AspNetCore.SignalR.Client;
using SyncDemo.Shared.Models;
using SyncDemo.Shared.DTOs;
using SyncDemo.WpfApp.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SyncDemo.WpfApp.Services;

public class DataUpdatedEventArgs : EventArgs
{
  public string EntityType { get; set; } = string.Empty;
  public string Operation { get; set; } = string.Empty;
}

public interface ISyncService
{
  Task ConnectAsync(string deviceId, string username);
  Task DisconnectAsync();
  bool IsConnected { get; }
  event EventHandler<DataUpdatedEventArgs>? DataUpdated;
}

public class SyncService : ISyncService
{
  private readonly IRealmService _realmService;
  private readonly IHttpClientFactory _httpClientFactory;
  private HubConnection? _hubConnection;
  private readonly string _hubUrl = "http://localhost:5000/synchub";
  private readonly string _apiBaseUrl = "http://localhost:5000/api/";
  private string _deviceId = string.Empty;

  public event EventHandler<DataUpdatedEventArgs>? DataUpdated;
  public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

  public SyncService(IRealmService realmService, IHttpClientFactory httpClientFactory)
  {
    _realmService = realmService;
    _httpClientFactory = httpClientFactory;
  }

  public async Task ConnectAsync(string deviceId, string username)
  {
    _deviceId = deviceId;

    if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
      return;

    // First, register the device
    await RegisterDeviceAsync(deviceId, username);

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
      var httpClient = _httpClientFactory.CreateClient();
      httpClient.BaseAddress = new Uri(_apiBaseUrl);

      // Get the last sync time (simplified - in production, store this)
      var lastSyncTime = DateTime.UtcNow.AddDays(-30);

      var timestring = $"{lastSyncTime:O}";
      // Fetch updates from server with device ID for permission filtering
      var response = await httpClient.GetAsync($"/api/syncitems/sync?since={timestring}&deviceId={_deviceId}");
      
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

  private async Task RegisterDeviceAsync(string deviceId, string username)
  {
    try
    {
      var httpClient = _httpClientFactory.CreateClient();
      httpClient.BaseAddress = new Uri(_apiBaseUrl);

      var registrationRequest = new DeviceRegistrationRequest
      {
        DeviceId = deviceId,
        DeviceName = Environment.MachineName,
        DeviceType = "WPF",
        Username = username
      };

      var json = JsonSerializer.Serialize(registrationRequest);
      var content = new StringContent(json, Encoding.UTF8, "application/json");

      var response = await httpClient.PostAsync("/api/device/register", content);

      if (!response.IsSuccessStatusCode)
      {
        System.Diagnostics.Debug.WriteLine("Failed to register device");
        throw new Exception("Device registration failed");
      }

      var registrationResponse = await response.Content.ReadFromJsonAsync<DeviceRegistrationResponse>();

      if (registrationResponse != null && registrationResponse.Success)
      {
        System.Diagnostics.Debug.WriteLine($"Device registered with {registrationResponse.Permissions.Count} permissions");
        foreach (var perm in registrationResponse.Permissions)
        {
          System.Diagnostics.Debug.WriteLine($"  - {perm.EntityType} ({perm.PermissionType})");
        }
      }
      else
      {
        throw new Exception(registrationResponse?.Message ?? "Device registration failed");
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Device registration error: {ex.Message}");
      throw;
    }
  }
}
