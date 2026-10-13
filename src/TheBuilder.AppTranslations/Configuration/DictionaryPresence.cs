using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.AppTranslations.Configuration;

/// <summary>
/// Whether the site has any Dictionary items, which every backoffice load asks to decide whether
/// Umbraco's Translation section shows. Cached until the Dictionary changes, since it rarely does.
/// </summary>
public sealed class DictionaryPresence(IDictionaryItemService dictionaryItems, AppCaches appCaches)
{
    internal const string CacheKey = Constants.PackageName + ".HasDictionaryItems";

    public async Task<bool> HasItemsAsync()
    {
        if (appCaches.RuntimeCache.Get(CacheKey) is bool cached)
            return cached;

        // Every Dictionary item descends from a root one, so no roots means none at all.
        var hasItems = await dictionaryItems.CountRootAsync() > 0;
        appCaches.RuntimeCache.Insert(CacheKey, () => hasItems);
        return hasItems;
    }
}

/// <summary>
/// Drops the cached answer whenever the Dictionary changes. The cache refresher notification rather
/// than the saved and deleted ones, because it reaches every server in a load-balanced site.
/// </summary>
internal sealed class DictionaryPresenceInvalidator(AppCaches appCaches)
    : INotificationHandler<DictionaryCacheRefresherNotification>
{
    public void Handle(DictionaryCacheRefresherNotification notification) =>
        appCaches.RuntimeCache.Clear(DictionaryPresence.CacheKey);
}
