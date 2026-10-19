using TheBuilder.AppTranslations.Persistence;
using Umbraco.Cms.Infrastructure.Migrations;

namespace TheBuilder.AppTranslations.Migrations;

/// <summary>The column each source keeps its sync schedule in. Null, the default, is no schedule.</summary>
internal sealed class AddSourceSyncInterval(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!ColumnExists(Constants.Tables.Sources, nameof(SourceRow.SyncIntervalMinutes)))
            AddColumn<SourceRow>(Constants.Tables.Sources, nameof(SourceRow.SyncIntervalMinutes));
        return Task.CompletedTask;
    }
}
