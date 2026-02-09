using CommunityToolkit.Mvvm.ComponentModel;
using SyncDemo.MauiApp.Models;

namespace SyncDemo.MauiApp.ViewModels;

public partial class SyncItemViewModel : ObservableObject
{
    [ObservableProperty]
    private RealmSyncItem _item = new();

    public void SetItem(RealmSyncItem item)
    {
        Item = item;
    }
}
