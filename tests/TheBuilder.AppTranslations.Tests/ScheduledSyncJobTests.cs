using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TheBuilder.AppTranslations.Configuration;
using TheBuilder.AppTranslations.Core.Messages;
using TheBuilder.AppTranslations.Core.Persistence;
using TheBuilder.AppTranslations.Core.Sources;
using TheBuilder.AppTranslations.Core.Synchronization;
using TheBuilder.AppTranslations.Synchronization;

namespace TheBuilder.AppTranslations.Tests;

public sealed class ScheduledSyncJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Daily = TimeSpan.FromDays(1);

    [Fact]
    public async Task Does_nothing_without_an_interval()
    {
        var source = Source();
        var (job, engine) = Create(interval: null, [source]);

        await job.RunJobAsync();

        Assert.Empty(engine.Synced);
    }

    [Fact]
    public async Task Syncs_a_source_that_has_never_synced()
    {
        var source = Source();
        var (job, engine) = Create(Daily, [source]);

        await job.RunJobAsync();

        Assert.Equal([source.Alias], engine.Synced);
    }

    [Fact]
    public async Task Syncs_a_source_once_the_interval_has_passed_since_its_last_sync()
    {
        var due = Source("due");
        var recent = Source("recent");
        var (job, engine) = Create(
            Daily,
            [due, recent],
            [Status(due, startedAt: Now - Daily), Status(recent, startedAt: Now - TimeSpan.FromHours(23))]);

        await job.RunJobAsync();

        Assert.Equal(["due"], engine.Synced);
    }

    [Fact]
    public async Task Counts_a_failed_sync_so_a_broken_source_is_not_retried_every_check()
    {
        var source = Source();
        var (job, engine) = Create(Daily, [source], [Status(source, startedAt: Now - TimeSpan.FromMinutes(10), TranslationSyncStatus.Failed)]);

        await job.RunJobAsync();

        Assert.Empty(engine.Synced);
    }

    [Fact]
    public async Task Skips_sources_that_are_turned_off_or_already_syncing()
    {
        var disabled = Source("disabled", enabled: false);
        var busy = Source("busy");
        var (job, engine) = Create(Daily, [disabled, busy], [Status(busy, startedAt: Now - Daily * 2) with { SyncInProgress = true }]);

        await job.RunJobAsync();

        Assert.Empty(engine.Synced);
    }

    [Fact]
    public async Task One_failing_source_does_not_stop_the_others()
    {
        var broken = Source("broken");
        var working = Source("working");
        var (job, engine) = Create(Daily, [broken, working]);
        engine.Failing.Add("broken");

        await job.RunJobAsync();

        Assert.Equal(["broken", "working"], engine.Synced);
    }

    [Fact]
    public void The_next_sync_is_the_interval_after_the_last_one_or_now_when_due()
    {
        var source = Source();

        Assert.Null(ScheduledSyncJob.NextSync(source, null, null, Now));
        Assert.Null(ScheduledSyncJob.NextSync(source with { Enabled = false }, null, Daily, Now));
        Assert.Equal(Now, ScheduledSyncJob.NextSync(source, null, Daily, Now));
        Assert.Equal(Now, ScheduledSyncJob.NextSync(source, Status(source, Now - Daily * 3), Daily, Now));
        Assert.Equal(Now + TimeSpan.FromHours(1), ScheduledSyncJob.NextSync(source, Status(source, Now - TimeSpan.FromHours(23)), Daily, Now));
        Assert.Equal(Now + TimeSpan.FromHours(2), ScheduledSyncJob.NextSync(source with { LastSuccessfulSync = Now - TimeSpan.FromHours(22) }, null, Daily, Now));
    }

    private static (ScheduledSyncJob Job, FakeEngine Engine) Create(
        TimeSpan? interval,
        IReadOnlyList<TranslationSourceDefinition> sources,
        IReadOnlyList<TranslationSourceStatus>? statuses = null)
    {
        var engine = new FakeEngine();
        var services = new ServiceCollection()
            .AddSingleton<ITranslationSourceRepository>(new FakeSources(sources, statuses ?? []))
            .AddSingleton<ITranslationSyncEngine>(engine)
            .BuildServiceProvider();
        var options = new FixedOptions(new ScheduledSyncOptions { Interval = interval });
        var job = new ScheduledSyncJob(options, services.GetRequiredService<IServiceScopeFactory>(), new FixedTime(), NullLogger<ScheduledSyncJob>.Instance);
        return (job, engine);
    }

    private static TranslationSourceDefinition Source(string alias = "website", bool enabled = true) => new(
        Guid.NewGuid(), alias, alias, enabled,
        new HttpTranslationTransportOptions("https://example.com/{locale}.json"),
        new TranslationParserOptions(["en"], alias, NamespaceMode.Fixed, MessageFormat.Icu));

    private static TranslationSourceStatus Status(
        TranslationSourceDefinition source,
        DateTimeOffset startedAt,
        TranslationSyncStatus status = TranslationSyncStatus.Succeeded) =>
        new(source.Id, false, null, new TranslationSyncResult(Guid.NewGuid(), source.Id, "r", startedAt, startedAt, status, 0, 0, 0, 0));

    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class FixedOptions(ScheduledSyncOptions value) : IOptionsMonitor<ScheduledSyncOptions>
    {
        public ScheduledSyncOptions CurrentValue => value;
        public ScheduledSyncOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<ScheduledSyncOptions, string?> listener) => null;
    }

    private sealed class FakeEngine : ITranslationSyncEngine
    {
        public List<string> Synced { get; } = [];
        public HashSet<string> Failing { get; } = [];

        public Task<TranslationSyncResult> SynchronizeAsync(TranslationSourceDefinition source, CancellationToken cancellationToken)
        {
            Synced.Add(source.Alias);
            if (Failing.Contains(source.Alias))
                throw new TranslationSourceFormatException("No messages.");
            return Task.FromResult(new TranslationSyncResult(Guid.NewGuid(), source.Id, "r", Now, Now, TranslationSyncStatus.Succeeded, 0, 0, 0, 0));
        }

        public Task<TranslationSourceTestResult> TestAsync(TranslationSourceDefinition source, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeSources(
        IReadOnlyList<TranslationSourceDefinition> sources,
        IReadOnlyList<TranslationSourceStatus> statuses) : ITranslationSourceRepository
    {
        public Task<IReadOnlyList<TranslationSourceDefinition>> GetSourcesAsync(CancellationToken cancellationToken) => Task.FromResult(sources);
        public Task<IReadOnlyList<TranslationSourceStatus>> GetSourceStatusesAsync(CancellationToken cancellationToken) => Task.FromResult(statuses);
        public Task<TranslationSourceDefinition?> GetSourceAsync(Guid sourceId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TranslationSourceDefinition?> GetSourceByAliasAsync(string alias, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SaveSourceAsync(TranslationSourceDefinition source, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteSourceAsync(Guid sourceId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TranslationSyncResult>> GetSyncHistoryAsync(Guid sourceId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
