using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheBuilder.AppTranslations.Core.Persistence;
using TheBuilder.AppTranslations.Core.Sources;
using TheBuilder.AppTranslations.Core.Synchronization;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace TheBuilder.AppTranslations.Synchronization;

/// <summary>
/// Syncs each enabled source that has a schedule once its last sync is older than its interval.
///
/// The interval counts from the last sync of any kind, rather than from when the site started, so a
/// deployment that restarts the site or syncs the source itself does not set off another sync, and a
/// daily schedule stays daily however often the site restarts. A failed sync counts too: a source
/// that is down is tried again at the next interval, not every few minutes.
///
/// Umbraco runs it on the Single or SchedulingPublisher server only, and the lease every sync holds
/// keeps it from running beside a sync someone started meanwhile.
/// </summary>
internal sealed class ScheduledSyncJob(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<ScheduledSyncJob> logger) : IRecurringBackgroundJob
{
    /// <summary>
    /// When this job last started each source. A sync that times out is cancelled before the engine
    /// records it, so without this a source that hangs would be tried again at every check.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _attempts = new();

    public TimeSpan Period => TimeSpan.FromMinutes(TranslationSourceValidator.MinimumSyncIntervalMinutes);

    // The period never changes. A changed schedule is read on the next check instead.
    public event EventHandler PeriodChanged { add { } remove { } }

    public async Task RunJobAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var sources = scope.ServiceProvider.GetRequiredService<ITranslationSourceRepository>();
        var syncEngine = scope.ServiceProvider.GetRequiredService<ITranslationSyncEngine>();
        // Most sites schedule nothing, and the statuses read the sync history, so look at that only
        // when some source has a schedule.
        var scheduled = (await sources.GetSourcesAsync(CancellationToken.None))
            .Where(source => source is { Enabled: true, SyncIntervalMinutes: not null })
            .ToArray();
        if (scheduled.Length == 0)
            return;
        var statuses = (await sources.GetSourceStatusesAsync(CancellationToken.None)).ToDictionary(status => status.SourceId);
        var now = timeProvider.GetUtcNow();

        foreach (var source in scheduled)
        {
            var status = statuses.GetValueOrDefault(source.Id);
            if (status?.SyncInProgress == true || NextSync(source, status, now) is not { } next || next > now)
                continue;
            if (_attempts.TryGetValue(source.Id, out var attempted) && attempted.AddMinutes(source.SyncIntervalMinutes!.Value) > now)
                continue;
            _attempts[source.Id] = now;

            try
            {
                await syncEngine.SynchronizeAsync(source, CancellationToken.None);
            }
            catch (TranslationSynchronizationInProgressException)
            {
                // Someone else started one meanwhile, which is as good as this one.
            }
            catch (Exception exception)
            {
                // The sync history already has the reason, where an editor will look for it.
                logger.LogWarning(exception, "Scheduled synchronization failed for source {SourceAlias}.", source.Alias);
            }
        }
    }

    /// <summary>
    /// When the schedule syncs this source next, or null when it has none or is turned off. A source
    /// that is due, or has never synced, is due <paramref name="now"/>: at the next check.
    /// </summary>
    public static DateTimeOffset? NextSync(TranslationSourceDefinition source, TranslationSourceStatus? status, DateTimeOffset now)
    {
        if (source.SyncIntervalMinutes is not { } minutes || !source.Enabled)
            return null;
        var lastSync = status?.LastSync?.StartedAt ?? source.LastSuccessfulSync;
        var next = lastSync + TimeSpan.FromMinutes(minutes);
        return next is null || next <= now ? now : next;
    }
}
