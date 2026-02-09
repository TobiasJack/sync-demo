using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SyncDemo.MauiApp.Data;
using SyncDemo.MauiApp.Models;
using SyncDemo.MauiApp.Services;

namespace SyncDemo.MauiApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IRealmService _realmService;
    private readonly ISyncService _syncService;
    private readonly ISignalRService _signalRService;

    [ObservableProperty]
    private ObservableCollection<RealmSyncItem> _items = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _deviceId = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _username = "user1";

    [ObservableProperty]
    private bool _isRegistered = false;

    public MainViewModel(IRealmService realmService, ISyncService syncService, ISignalRService signalRService)
    {
        _realmService = realmService;
        _syncService = syncService;
        _signalRService = signalRService;

        // Subscribe to SignalR updates
        _signalRService.OnSyncUpdateReceived += OnSyncUpdateReceived;
    }

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Loading items...";

            Items.Clear();
            var items = _realmService.GetAllItems().ToList();
            
            foreach (var item in items)
            {
                Items.Add(item);
            }

            StatusMessage = $"Loaded {items.Count} items";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RegisterDeviceAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Registering device...";

            var success = await _syncService.RegisterDeviceAsync(DeviceId, Username);
            
            if (success)
            {
                IsRegistered = true;
                StatusMessage = $"Device registered as {Username}";
            }
            else
            {
                StatusMessage = "Device registration failed";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Registration error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SyncWithServerAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Syncing with server...";

            // Connect to SignalR
            await _signalRService.StartAsync();

            // Perform sync with device ID
            var result = await _syncService.SyncWithServerAsync(DeviceId);
            
            if (result.Success)
            {
                StatusMessage = result.Message;
                await LoadItemsAsync();
            }
            else
            {
                StatusMessage = $"Sync failed: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sync error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddItemAsync()
    {
        var newItem = new RealmSyncItem
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"Item {DateTime.Now:HH:mm:ss}",
            Description = "New item created from mobile app",
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        var success = await _syncService.CreateItemAsync(newItem);
        
        if (success)
        {
            Items.Add(newItem);
            StatusMessage = "Item created successfully";
        }
        else
        {
            StatusMessage = "Failed to create item";
        }
    }

    [RelayCommand]
    private async Task DeleteItemAsync(RealmSyncItem item)
    {
        if (item == null) return;

        var success = await _syncService.DeleteItemAsync(item.Id);
        
        if (success)
        {
            Items.Remove(item);
            StatusMessage = "Item deleted successfully";
        }
        else
        {
            StatusMessage = "Failed to delete item";
        }
    }

    private void OnSyncUpdateReceived(Shared.Models.SyncMessage message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            StatusMessage = $"Received {message.Operation} update from server";
            await LoadItemsAsync();
        });
    }
}
