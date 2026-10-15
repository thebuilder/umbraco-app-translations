using TheBuilder.AppTranslations.Core.Output;

namespace TheBuilder.AppTranslations.Core.Persistence;

public interface ITranslationMessageRepository
{
    Task<Page<TranslationMessageView>> QueryMessagesAsync(MessageQuery query, CancellationToken cancellationToken);
    Task<TranslationFacetData> GetFacetDataAsync(CancellationToken cancellationToken);
    Task<TranslationMessageView?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken);
    Task<TranslationOverride> SaveOverrideAsync(Guid messageId, string value, long? expectedVersion, string userId, CancellationToken cancellationToken);
    Task DeleteOverrideAsync(Guid messageId, long? expectedVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<TranslationMessageView>> GetOutputMessagesAsync(string locale, string messageNamespace, CancellationToken cancellationToken);

    /// <summary>
    /// The messages the delivery API serves for a locale, from the namespaces named or, when there
    /// are none, from every namespace.
    ///
    /// The default asks for each namespace in turn, so a repository written before this overload
    /// existed still serves a request that names its namespaces. It has no way to list every
    /// namespace, so serving a request for all of them needs this implemented.
    /// </summary>
    async Task<IReadOnlyList<TranslationMessageView>> GetOutputMessagesAsync(
        string locale,
        IReadOnlyCollection<string> namespaces,
        CancellationToken cancellationToken)
    {
        if (namespaces.Count == 0)
            throw new NotSupportedException($"{GetType().Name} cannot list every namespace. Implement {nameof(GetOutputMessagesAsync)} with a namespace list to serve requests that leave the namespace out.");
        var messages = new List<TranslationMessageView>();
        foreach (var messageNamespace in namespaces)
            messages.AddRange(await GetOutputMessagesAsync(locale, messageNamespace, cancellationToken));
        return messages;
    }
}

public sealed class TranslationConcurrencyException(string message) : Exception(message);
