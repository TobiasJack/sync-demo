using Realms;
using SyncDemo.WpfApp.Models;

namespace SyncDemo.WpfApp.Services;

public interface IRealmService
{
    Task<IEnumerable<RealmSyncItem>> GetAllItemsAsync();
    RealmSyncItem? GetItemById(string id);
    Task<RealmSyncItem> AddOrUpdateItemAsync(RealmSyncItem item);
    Task DeleteItemAsync(string id);
    Task<int> GetItemCountAsync();
}

public class RealmService : IRealmService
{
    private readonly Realm _realm;

    public RealmService()
    {
        var config = new RealmConfiguration("syncdemo-wpf.realm")
        {
            SchemaVersion = 1
        };
        
        _realm = Realm.GetInstance(config);
    }

    public Task<IEnumerable<RealmSyncItem>> GetAllItemsAsync()
    {
        var items = _realm.All<RealmSyncItem>().Where(i => !i.IsDeleted).ToList();
        return Task.FromResult<IEnumerable<RealmSyncItem>>(items);
    }

    public RealmSyncItem? GetItemById(string id)
    {
        return _realm.Find<RealmSyncItem>(id);
    }

    public async Task<RealmSyncItem> AddOrUpdateItemAsync(RealmSyncItem item)
    {
        await _realm.WriteAsync(() =>
        {
            _realm.Add(item, update: true);
        });
        
        return item;
    }

    public async Task DeleteItemAsync(string id)
    {
        await _realm.WriteAsync(() =>
        {
            var item = _realm.Find<RealmSyncItem>(id);
            if (item != null)
            {
                item.IsDeleted = true;
                item.ModifiedAt = DateTimeOffset.UtcNow;
            }
        });
    }

    public Task<int> GetItemCountAsync()
    {
        return Task.FromResult(_realm.All<RealmSyncItem>().Count(i => !i.IsDeleted));
    }
}
