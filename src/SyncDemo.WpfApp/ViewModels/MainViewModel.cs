using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SyncDemo.WpfApp.Services;

namespace SyncDemo.WpfApp.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ISyncService _syncService;

    [ObservableProperty]
    private string _deviceId = Guid.NewGuid().ToString();

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusText = "Nicht verbunden";

    public SyncItemsViewModel? SyncItemsViewModel { get; set; }

    public MainViewModel(ISyncService syncService)
    {
        _syncService = syncService;
        _syncService.DataUpdated += OnDataUpdated;
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        try
        {
            StatusText = "Verbinde...";
            await _syncService.ConnectAsync(DeviceId);
            IsConnected = true;
            StatusText = $"Verbunden als {DeviceId}";
        }
        catch (Exception ex)
        {
            StatusText = $"Fehler: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _syncService.DisconnectAsync();
        IsConnected = false;
        StatusText = "Nicht verbunden";
    }

    private void OnDataUpdated(object? sender, DataUpdatedEventArgs e)
    {
        StatusText = $"Update empfangen: {e.EntityType} ({e.Operation}) - {DateTime.Now:HH:mm:ss}";
    }

    public void Dispose()
    {
        _syncService.DataUpdated -= OnDataUpdated;
    }
}
