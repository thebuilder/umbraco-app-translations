using System.Collections.Concurrent;
using TheBuilder.AppTranslations.Core.Output;

namespace TheBuilder.AppTranslations.DeliveryApi;

/// <summary>Which response: the same four values always build the same JSON.</summary>
internal readonly record struct DeliveryResponseKey(
    TranslationOutputMode Mode,
    TranslationOutputFormat Format,
    string Locale,
    string Namespace);

/// <summary>A built response, with the ETag that names it.</summary>
internal sealed record CachedDeliveryResponse(string Json, string ETag);

/// <summary>
/// The delivery endpoints' responses, kept until the translations change.
///
/// Without it every request ran the query, serialized the JSON and hashed it, and a 304 did all of
/// that before it could say nothing had changed. With it, a repeat request and a revalidation cost a
/// dictionary lookup, so a client can revalidate every time and see an edit the moment it is saved.
///
/// <see cref="DeliveryResponseCacheRefresher"/> clears it on every server whenever a change
/// commits. A request that read the database before the change, and finishes after the clear, must
/// not put its old response back: <see cref="Generation"/> counts the clears, and a response is
/// stored only if no clear happened while it was being built.
/// </summary>
public sealed class DeliveryResponseCache
{
    private readonly ConcurrentDictionary<DeliveryResponseKey, CachedDeliveryResponse> _responses = new();
    private readonly Lock _gate = new();
    private long _generation;

    /// <summary>Read before building a response, and handed back to <see cref="Store"/>.</summary>
    internal long Generation => Interlocked.Read(ref _generation);

    internal bool TryGet(DeliveryResponseKey key, out CachedDeliveryResponse response) =>
        _responses.TryGetValue(key, out response!);

    internal void Store(DeliveryResponseKey key, CachedDeliveryResponse response, long generationAtRead)
    {
        lock (_gate)
        {
            if (generationAtRead == _generation)
                _responses[key] = response;
        }
    }

    internal void Clear()
    {
        lock (_gate)
        {
            Interlocked.Increment(ref _generation);
            _responses.Clear();
        }
    }
}
