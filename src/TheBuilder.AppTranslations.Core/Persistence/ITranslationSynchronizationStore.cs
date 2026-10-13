using TheBuilder.AppTranslations.Core.Sources;
using TheBuilder.AppTranslations.Core.Synchronization;

namespace TheBuilder.AppTranslations.Core.Persistence;

public interface ITranslationSynchronizationStore
{
    Task<TranslationSynchronizationLease?> TryAcquireSynchronizationLeaseAsync(
        TranslationSourceDefinition source,
        TimeSpan duration,
        CancellationToken cancellationToken);

    Task<TranslationSourceDefinition?> GetSynchronizationSourceAsync(
        TranslationSynchronizationLease lease,
        CancellationToken cancellationToken);

    Task ReleaseSynchronizationLeaseAsync(
        TranslationSynchronizationLease lease,
        CancellationToken cancellationToken);

    Task<bool> RenewSynchronizationLeaseAsync(
        TranslationSynchronizationLease lease,
        TimeSpan duration,
        CancellationToken cancellationToken);

    Task<TranslationSyncResult> ApplySynchronizationAsync(
        TranslationSourceDefinition source,
        TranslationSynchronizationLease lease,
        IReadOnlyCollection<TranslationSourceMessage> messages,
        string revision,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken);

    Task RecordFailedSynchronizationAsync(TranslationSyncResult result, CancellationToken cancellationToken);
}

public sealed record TranslationSynchronizationLease(Guid SourceId, Guid LeaseId);
