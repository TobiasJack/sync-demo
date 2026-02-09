using Realms;
using Realms.Exceptions;

using SyncDemo.WpfApp.Models;

namespace SyncDemo.WpfApp.Services;

public interface IRealmService : IDisposable
{
  Task<IEnumerable<RealmSyncItem>> GetAllItemsAsync();
  RealmSyncItem? GetItemById(string id);
  Task<RealmSyncItem> AddOrUpdateItemAsync(RealmSyncItem item);
  Task DeleteItemAsync(string id);
  Task<int> GetItemCountAsync();
}

public class RealmService : IRealmService
{
  public RealmService()
  {
  }

  private Realm GetRealmInstance()
  {
    var config = new RealmConfiguration("syncdemo-wpf.realm")
    {
      SchemaVersion = 1,
      ShouldDeleteIfMigrationNeeded = true
    };

    return Realm.GetInstance(config);
  }

  public Task<IEnumerable<RealmSyncItem>> GetAllItemsAsync()
  {
    var items = GetRealmInstance().All<RealmSyncItem>().Where(i => !i.IsDeleted).ToList().Select(x => x.Detach()).ToList();
    return Task.FromResult<IEnumerable<RealmSyncItem>>(items);
  }

  public RealmSyncItem? GetItemById(string id)
  {
    return GetRealmInstance().Find<RealmSyncItem>(id);
  }

  public async Task<RealmSyncItem> AddOrUpdateItemAsync(RealmSyncItem item)
  {
    var realm = GetRealmInstance();

    await realm.WriteAsync(() =>
    {
      realm.Add(item, update: true);
    });

    return item;
  }

  public async Task DeleteItemAsync(string id)
  {
    var realm = GetRealmInstance();
    await realm.WriteAsync(() =>
    {
      var item = realm.Find<RealmSyncItem>(id);
      if (item != null)
      {
        item.IsDeleted = true;
        item.ModifiedAt = DateTimeOffset.UtcNow;
      }
    });
  }

  public Task<int> GetItemCountAsync()
  {
    return Task.FromResult(GetRealmInstance().All<RealmSyncItem>().Count(i => !i.IsDeleted));
  }

  public void Dispose()
  {
  }
}
