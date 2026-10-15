using TheBuilder.AppTranslations.Core.Output;
using TheBuilder.AppTranslations.DeliveryApi;
using TheBuilder.AppTranslations.Notifications;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;

namespace TheBuilder.AppTranslations.Tests;

public sealed class DeliveryResponseCacheTests
{
    private static readonly DeliveryResponseKey Key = new(TranslationOutputMode.All, TranslationOutputFormat.Icu, "da-DK", DeliveryNamespaces.From(["website"]));
    private static readonly CachedDeliveryResponse Response = new("{}", "\"etag\"");

    [Fact]
    public void A_stored_response_is_found_until_the_cache_is_cleared()
    {
        var cache = new DeliveryResponseCache(TimeProvider.System);
        cache.Store(Key, Response, cache.Generation);

        Assert.True(cache.TryGet(Key, out var found));
        Assert.Same(Response, found);

        cache.Clear();
        Assert.False(cache.TryGet(Key, out _));
    }

    [Fact]
    public void Past_the_limit_the_oldest_set_of_namespaces_makes_room()
    {
        var clock = new Clock();
        var cache = new DeliveryResponseCache(clock);
        DeliveryResponseKey Set(int index) =>
            new(TranslationOutputMode.All, TranslationOutputFormat.Icu, "da-DK", DeliveryNamespaces.From(["website", $"ns{index}"]));
        void StoreAt(DeliveryResponseKey key)
        {
            clock.Now += TimeSpan.FromSeconds(1);
            cache.Store(key, Response, cache.Generation);
        }

        for (var index = 0; index <= DeliveryResponseCache.MaximumNamespaceSets; index++)
            StoreAt(Set(index));

        Assert.False(cache.TryGet(Set(0), out _));
        Assert.True(cache.TryGet(Set(1), out _));
        Assert.True(cache.TryGet(Set(DeliveryResponseCache.MaximumNamespaceSets), out _));

        // Storing a set that is already there again takes no one's place.
        StoreAt(Set(1));
        Assert.True(cache.TryGet(Set(2), out _));

        // A single namespace, and every namespace, are not sets and are never made to make room.
        StoreAt(Key);
        StoreAt(Set(DeliveryResponseCache.MaximumNamespaceSets + 1));
        Assert.True(cache.TryGet(Key, out _));
        Assert.False(cache.TryGet(Set(2), out _));
    }

    [Fact]
    public void The_same_namespaces_in_any_order_or_repeated_are_one_key()
    {
        Assert.Equal(DeliveryNamespaces.From(["website", "checkout"]), DeliveryNamespaces.From(["checkout", "website", "checkout", " "]));
        Assert.NotEqual(DeliveryNamespaces.From(["website"]), DeliveryNamespaces.From(["Website"]));
        Assert.Equal(DeliveryNamespaces.From(null), DeliveryNamespaces.From([""]));
    }

    [Fact]
    public void A_response_built_before_a_clear_is_not_put_back()
    {
        var cache = new DeliveryResponseCache(TimeProvider.System);
        var generationAtRead = cache.Generation;

        // A change commits, and clears the cache, while this response is still being built from
        // what the database held before it.
        cache.Clear();
        cache.Store(Key, Response, generationAtRead);

        Assert.False(cache.TryGet(Key, out _));
    }

    [Fact]
    public void A_response_is_dropped_after_its_lifetime()
    {
        var clock = new Clock();
        var cache = new DeliveryResponseCache(clock);
        cache.Store(Key, Response, cache.Generation);

        clock.Now += DeliveryResponseCache.Lifetime - TimeSpan.FromSeconds(1);
        Assert.True(cache.TryGet(Key, out _));

        clock.Now += TimeSpan.FromSeconds(1);
        Assert.False(cache.TryGet(Key, out _));
    }

    [Fact]
    public void The_refresher_clears_the_cache()
    {
        var cache = new DeliveryResponseCache(TimeProvider.System);
        cache.Store(Key, Response, cache.Generation);

        Refresher(cache).RefreshAll();

        Assert.False(cache.TryGet(Key, out _));
    }

    [Fact]
    public void A_committed_change_asks_every_server_to_clear_its_cache()
    {
        var cache = new DeliveryResponseCache(TimeProvider.System);
        var refresher = Refresher(cache);
        var messenger = new RecordingMessenger();
        var distributedCache = new DistributedCache(messenger, new CacheRefresherCollection(() => [refresher]));

        new DeliveryResponseCacheInvalidator(distributedCache).Handle(
            new(Guid.NewGuid(), "website", TranslationChange.OverrideSaved, DateTimeOffset.UtcNow));

        Assert.Equal([DeliveryResponseCacheRefresher.UniqueId], messenger.RefreshedAll);
    }

    private static DeliveryResponseCacheRefresher Refresher(DeliveryResponseCache cache) =>
        new(AppCaches.NoCache, new SilentEvents(), new NotificationFactory(), cache);

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    /// <summary>Records which refreshers were asked to refresh everything, on every server.</summary>
    private sealed class RecordingMessenger : IServerMessenger
    {
        public List<Guid> RefreshedAll { get; } = [];

        public void QueueRefreshAll(ICacheRefresher refresher) => RefreshedAll.Add(refresher.RefresherUniqueId);

        public void Sync() { }
        public void SendMessages() { }
        public void QueueRefresh<TPayload>(ICacheRefresher refresher, TPayload[] payload) => throw new NotSupportedException();
        public void QueueRefresh<T>(ICacheRefresher refresher, Func<T, int> getNumericId, params T[] instances) => throw new NotSupportedException();
        public void QueueRefresh<T>(ICacheRefresher refresher, Func<T, Guid> getGuidId, params T[] instances) => throw new NotSupportedException();
        public void QueueRemove<T>(ICacheRefresher refresher, Func<T, int> getNumericId, params T[] instances) => throw new NotSupportedException();
        public void QueueRemove(ICacheRefresher refresher, params int[] numericIds) => throw new NotSupportedException();
        public void QueueRefresh(ICacheRefresher refresher, params int[] numericIds) => throw new NotSupportedException();
        public void QueueRefresh(ICacheRefresher refresher, params Guid[] guidIds) => throw new NotSupportedException();
    }

    private sealed class NotificationFactory : ICacheRefresherNotificationFactory
    {
        public TNotification Create<TNotification>(object msgObject, MessageType type)
            where TNotification : CacheRefresherNotification =>
            (TNotification)Activator.CreateInstance(typeof(TNotification), msgObject, type)!;
    }

    private sealed class SilentEvents : IEventAggregator
    {
        public void Publish<TNotification>(TNotification notification)
            where TNotification : INotification { }

        public void Publish<TNotification, TNotificationHandler>(IEnumerable<TNotification> notifications)
            where TNotification : INotification
            where TNotificationHandler : INotificationHandler { }

        public Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;

        public Task PublishAsync<TNotification, TNotificationHandler>(IEnumerable<TNotification> notifications, CancellationToken cancellationToken = default)
            where TNotification : INotification
            where TNotificationHandler : INotificationHandler => Task.CompletedTask;

        public bool PublishCancelable<TCancelableNotification>(TCancelableNotification notification)
            where TCancelableNotification : ICancelableNotification => false;

        public Task<bool> PublishCancelableAsync<TCancelableNotification>(TCancelableNotification notification)
            where TCancelableNotification : ICancelableNotification => Task.FromResult(false);
    }
}
