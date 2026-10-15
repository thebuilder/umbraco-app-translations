using TheBuilder.AppTranslations.Core.Sources;

namespace TheBuilder.AppTranslations.Core.Synchronization;

/// <summary>
/// Synchronizes a translation source, or tests one. The backoffice and the deployment sync endpoint
/// use it, and a site can too: resolve it from a scope to sync a source from its own job, or to see
/// whether a source answers before saving it.
/// </summary>
public interface ITranslationSyncEngine
{
    /// <summary>
    /// Fetches every locale of the source and applies the result. Only one synchronization of a
    /// source runs at a time.
    /// </summary>
    /// <remarks>
    /// Once the run has started, any failure other than cancellation or
    /// <see cref="TranslationSynchronizationInProgressException"/> is recorded in the source's sync
    /// history before it is thrown. Besides the exceptions below, an
    /// <see cref="ITranslationSourceTransport"/> that throws
    /// <see cref="TranslationSourceNotModifiedException"/> fails the run with it, because a
    /// synchronization always asks for the whole source.
    /// </remarks>
    /// <exception cref="TranslationSynchronizationInProgressException">The source is already being synchronized, or another run took it over. Nothing is recorded.</exception>
    /// <exception cref="TranslationSourceFetchException">A locale could not be fetched or parsed.</exception>
    /// <exception cref="TranslationSourceFormatException">Every locale answered, but with no messages.</exception>
    /// <exception cref="InvalidOperationException">The source is disabled.</exception>
    Task<TranslationSyncResult> SynchronizeAsync(
        TranslationSourceDefinition source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Fetches and parses every locale of the source without saving anything, and reports what came
    /// back. A source that cannot be fetched is a result, not an exception.
    /// </summary>
    /// <remarks>
    /// Only whether the source answers is tested. The definition itself is not validated: its alias,
    /// endpoint, limits and request headers are checked when the backoffice saves it, and a
    /// definition built in code that passes here can still be refused there.
    /// </remarks>
    Task<TranslationSourceTestResult> TestAsync(
        TranslationSourceDefinition source,
        CancellationToken cancellationToken);
}
