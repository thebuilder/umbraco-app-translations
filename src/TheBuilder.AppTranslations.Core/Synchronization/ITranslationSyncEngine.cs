using TheBuilder.AppTranslations.Core.Sources;

namespace TheBuilder.AppTranslations.Core.Synchronization;

/// <summary>
/// Synchronizes a translation source, or tests one. The backoffice and the deployment sync endpoint
/// use it, and a site can too: resolve it from a scope to sync a source from its own job, or to
/// check a source's settings before saving them.
/// </summary>
public interface ITranslationSyncEngine
{
    /// <summary>
    /// Fetches every locale of the source and applies the result, or records the failed run and
    /// throws. Only one synchronization of a source runs at a time.
    /// </summary>
    /// <exception cref="TranslationSynchronizationInProgressException">The source is already being synchronized.</exception>
    /// <exception cref="TranslationSourceFetchException">A locale could not be fetched.</exception>
    /// <exception cref="TranslationSourceFormatException">The source answered with nothing usable.</exception>
    /// <exception cref="InvalidOperationException">The source is disabled.</exception>
    Task<TranslationSyncResult> SynchronizeAsync(
        TranslationSourceDefinition source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Fetches and parses every locale of the source without saving anything, and reports what came
    /// back. A source that cannot be fetched is a result, not an exception.
    /// </summary>
    Task<TranslationSourceTestResult> TestAsync(
        TranslationSourceDefinition source,
        CancellationToken cancellationToken);
}
