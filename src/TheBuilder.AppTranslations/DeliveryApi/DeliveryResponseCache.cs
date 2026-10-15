using System.Collections.Concurrent;
using TheBuilder.AppTranslations.Core.Output;

namespace TheBuilder.AppTranslations.DeliveryApi;

/// <summary>
/// Which response: the same four values always build the same JSON. The format does not change the
/// JSON, only whether it may be served, so it is part of the key to keep a response checked for one
/// format from being handed to a request for another.
/// </summary>
internal readonly record struct DeliveryResponseKey(
    TranslationOutputMode Mode,
    TranslationOutputFormat? Format,
    string Locale,
    DeliveryNamespaces Namespaces);

/// <summary>
/// The namespaces a request asked for, sorted and without repeats, so the same namespaces asked for
/// in another order are the same response. None is every namespace.
/// </summary>
internal sealed class DeliveryNamespaces : IEquatable<DeliveryNamespaces>
{
    private DeliveryNamespaces(string[] names) => Names = names;

    public IReadOnlyList<string> Names { get; }

    /// <summary>One namespace's messages sit at the top level. Any other choice has each namespace under its own name.</summary>
    public bool KeyedByNamespace => Names.Count != 1;

    /// <summary>A blank value, such as <c>namespace=</c> with nothing after it, names no namespace.</summary>
    public static DeliveryNamespaces From(IEnumerable<string?>? requested) =>
        new((requested ?? [])
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray());

    public bool Equals(DeliveryNamespaces? other) =>
        other is not null && Names.SequenceEqual(other.Names, StringComparer.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as DeliveryNamespaces);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var name in Names)
            hash.Add(name, StringComparer.Ordinal);
        return hash.ToHashCode();
    }
}

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
///
/// A response is also dropped after <see cref="Lifetime"/>, whatever happened. Something can change
/// the translations without this site hearing about it: a restored backup, a row edited by hand, a
/// lost cache instruction. The lifetime bounds how long that goes unnoticed, at the cost of one
/// rebuild per response per hour.
///
/// Responses for several namespaces are kept only up to <see cref="MaximumNamespaceSets"/> of them.
/// The namespaces that exist can be combined in more ways than there is memory for, and a client
/// only ever asks for a few of those. Past the limit, the set stored longest ago makes room. Expired
/// sets go first that way, and a burst of sets nobody asks for again cannot keep out the ones an
/// application keeps asking for: those come back on their next request.
/// </summary>
public sealed class DeliveryResponseCache(TimeProvider timeProvider)
{
    internal static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);
    internal const int MaximumNamespaceSets = 100;

    private readonly ConcurrentDictionary<DeliveryResponseKey, (CachedDeliveryResponse Response, DateTimeOffset StoredAt)> _responses = new();
    private readonly Lock _gate = new();
    private long _generation;
    private int _namespaceSets;

    /// <summary>Read before building a response, and handed back to <see cref="Store"/>.</summary>
    internal long Generation => Interlocked.Read(ref _generation);

    internal bool TryGet(DeliveryResponseKey key, out CachedDeliveryResponse response)
    {
        if (_responses.TryGetValue(key, out var entry) && timeProvider.GetUtcNow() - entry.StoredAt < Lifetime)
        {
            response = entry.Response;
            return true;
        }
        response = null!;
        return false;
    }

    internal void Store(DeliveryResponseKey key, CachedDeliveryResponse response, long generationAtRead)
    {
        lock (_gate)
        {
            if (generationAtRead != _generation)
                return;
            if (key.Namespaces.Names.Count > 1 && !_responses.ContainsKey(key))
            {
                if (_namespaceSets >= MaximumNamespaceSets)
                    RemoveOldestNamespaceSet();
                else
                    _namespaceSets++;
            }
            _responses[key] = (response, timeProvider.GetUtcNow());
        }
    }

    /// <summary>Called under the lock, only when the sets are at their limit.</summary>
    private void RemoveOldestNamespaceSet()
    {
        var oldest = _responses
            .Where(entry => entry.Key.Namespaces.Names.Count > 1)
            .MinBy(entry => entry.Value.StoredAt);
        _responses.TryRemove(oldest.Key, out _);
    }

    internal void Clear()
    {
        lock (_gate)
        {
            Interlocked.Increment(ref _generation);
            _responses.Clear();
            _namespaceSets = 0;
        }
    }
}
