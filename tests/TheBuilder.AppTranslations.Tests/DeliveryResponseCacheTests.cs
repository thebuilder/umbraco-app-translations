using TheBuilder.AppTranslations.Core.Output;
using TheBuilder.AppTranslations.DeliveryApi;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;

namespace TheBuilder.AppTranslations.Tests;

public sealed class DeliveryResponseCacheTests
{
    private static readonly DeliveryResponseKey Key = new(TranslationOutputMode.All, TranslationOutputFormat.NextIntl, "da-DK", "website");
    private static readonly CachedDeliveryResponse Response = new("{}", "\"etag\"");

    [Fact]
    public void A_stored_response_is_found_until_the_cache_is_cleared()
    {
        var cache = new DeliveryResponseCache();
        cache.Store(Key, Response, cache.Generation);

        Assert.True(cache.TryGet(Key, out var found));
        Assert.Same(Response, found);

        cache.Clear();
        Assert.False(cache.TryGet(Key, out _));
    }

    [Fact]
    public void A_response_built_before_a_clear_is_not_put_back()
    {
        var cache = new DeliveryResponseCache();
        var generationAtRead = cache.Generation;

        // A change commits, and clears the cache, while this response is still being built from
        // what the database held before it.
        cache.Clear();
        cache.Store(Key, Response, generationAtRead);

        Assert.False(cache.TryGet(Key, out _));
    }

    [Fact]
    public void The_refresher_clears_the_cache_for_every_kind_of_instruction()
    {
        var cache = new DeliveryResponseCache();
        var refresher = new DeliveryResponseCacheRefresher(AppCaches.NoCache, new SilentEvents(), new NotificationFactory(), cache);
        Action[] instructions = [refresher.RefreshAll, () => refresher.Refresh(1), () => refresher.Refresh(Guid.NewGuid()), () => refresher.Remove(1)];

        foreach (var instruction in instructions)
        {
            cache.Store(Key, Response, cache.Generation);
            instruction();
            Assert.False(cache.TryGet(Key, out _));
        }
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
