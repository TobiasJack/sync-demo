using Realms;

namespace SyncDemo.MauiApp.Models;

public class RealmSyncItem : RealmObject
{
    [PrimaryKey]
    [MapTo("_id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MapTo("name")]
    public string Name { get; set; } = string.Empty;

    [MapTo("description")]
    public string Description { get; set; } = string.Empty;

    [MapTo("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [MapTo("modifiedAt")]
    public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;

    [MapTo("isDeleted")]
    public bool IsDeleted { get; set; }

    [MapTo("version")]
    public int Version { get; set; }
}
