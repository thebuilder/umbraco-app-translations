using System.Text;
using System.Text.Json;
using TheBuilder.AppTranslations.Core.Output;
using TheBuilder.AppTranslations.Core.Persistence;

namespace TheBuilder.AppTranslations.DeliveryApi;

/// <summary>
/// The JSON a delivery request gets. One namespace is the serializer's output as it is. Several, or
/// every namespace, put each namespace under its own name, serialized on its own so namespaces that
/// share a key stay apart.
/// </summary>
internal static class DeliveryOutput
{
    public static string Serialize(
        ITranslationOutputSerializer serializer,
        IReadOnlyCollection<TranslationMessageView> messages,
        TranslationOutputMode mode,
        DeliveryNamespaces namespaces)
    {
        if (!namespaces.KeyedByNamespace)
            return serializer.Serialize(messages, mode);

        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var json = new StringBuilder("{");
        foreach (var group in messages
            .GroupBy(view => OutputName(view.Message.Identity.Namespace, namespaces.Names, names), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            // From overrides, a namespace nobody edited has nothing to say, and is left out rather
            // than answered with an empty object.
            if (mode is TranslationOutputMode.Overrides && group.All(view => view.Override is null))
                continue;
            if (json.Length > 1)
                json.Append(',');
            json.Append('"').Append(JsonEncodedText.Encode(group.Key)).Append("\":").Append(serializer.Serialize(group.ToArray(), mode));
        }
        return json.Append('}').ToString();
    }

    /// <summary>
    /// The name a namespace is answered under: the spelling the request used for it. On a
    /// case-insensitive database <c>Checkout</c> finds the messages stored under <c>checkout</c>,
    /// and the application then looks for them under <c>Checkout</c>. With every namespace, nothing
    /// was spelled, so the stored name is used.
    /// </summary>
    private static string OutputName(string stored, IReadOnlyList<string> requested, Dictionary<string, string> names)
    {
        if (names.TryGetValue(stored, out var name))
            return name;
        name = requested.FirstOrDefault(candidate => string.Equals(candidate, stored, StringComparison.Ordinal))
            ?? requested.FirstOrDefault(candidate => string.Equals(candidate, stored, StringComparison.OrdinalIgnoreCase))
            ?? stored;
        names[stored] = name;
        return name;
    }
}
