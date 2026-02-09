using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using SyncDemo.WpfApp.Models;
using SyncDemo.WpfApp.Services;
using System.Windows;

namespace SyncDemo.WpfApp.ViewModels;

public partial class SyncItemsViewModel : ObservableObject, IDisposable
{
    private readonly IRealmService _realmService;
    private readonly ISyncService _syncService;

    [ObservableProperty]
    private ObservableCollection<RealmSyncItem> _items = new();

    public SyncItemsViewModel(IRealmService realmService, ISyncService syncService)
    {
        _realmService = realmService;
        _syncService = syncService;
        _syncService.DataUpdated += OnDataUpdated;
        
        LoadItems();
    }

    private async void LoadItems()
    {
        try
        {
            var items = await _realmService.GetAllItemsAsync();
            
            // Update UI on main thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                Items = new ObservableCollection<RealmSyncItem>(items);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadItems error: {ex.Message}");
        }
    }

    private void OnDataUpdated(object? sender, DataUpdatedEventArgs e)
    {
        if (e.EntityType == "ITEMS" || e.EntityType == "ALL")
        {
            LoadItems();
        }
    }

    public void Dispose()
    {
        _syncService.DataUpdated -= OnDataUpdated;
    }
}
