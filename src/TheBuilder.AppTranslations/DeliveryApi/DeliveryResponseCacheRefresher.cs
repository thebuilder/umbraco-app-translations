using TheBuilder.AppTranslations.Notifications;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;

namespace TheBuilder.AppTranslations.DeliveryApi;

/// <summary>
/// Clears <see cref="DeliveryResponseCache"/> on every server. A change commits on one server, and
/// Umbraco's distributed cache carries the instruction to the others, so a load-balanced site never
/// serves a response that one of its servers knows is stale.
/// </summary>
internal sealed class DeliveryResponseCacheRefresher : CacheRefresherBase<DeliveryResponseCacheRefresherNotification>
{
    public static readonly Guid UniqueId = new("4f1a9c2e-7b3d-4e8a-9d61-2c5f0b8a7e43");

    private readonly DeliveryResponseCache _cache;

    public DeliveryResponseCacheRefresher(
        AppCaches appCaches,
        IEventAggregator eventAggregator,
        ICacheRefresherNotificationFactory factory,
        DeliveryResponseCache cache)
        : base(appCaches, eventAggregator, factory) =>
        _cache = cache;

    public override Guid RefresherUniqueId => UniqueId;

    public override string Name => "App Translations delivery responses";

    // Every change clears everything: a sync can touch any locale and namespace, edits are rare next
    // to reads, and the next request for each response rebuilds it once.
    public override void RefreshAll()
    {
        _cache.Clear();
        base.RefreshAll();
    }

    public override void Refresh(int id) => RefreshAll();

    public override void Refresh(Guid id) => RefreshAll();

    public override void Remove(int id) => RefreshAll();
}

internal sealed class DeliveryResponseCacheRefresherNotification(object messageObject, MessageType messageType)
    : CacheRefresherNotification(messageObject, messageType);

/// <summary>Turns every committed change into a cluster-wide clear.</summary>
internal sealed class DeliveryResponseCacheInvalidator(DistributedCache distributedCache)
    : INotificationHandler<TranslationsUpdatedNotification>
{
    public void Handle(TranslationsUpdatedNotification notification) =>
        distributedCache.RefreshAll(DeliveryResponseCacheRefresher.UniqueId);
}
