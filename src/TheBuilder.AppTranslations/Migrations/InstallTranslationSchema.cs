using Umbraco.Cms.Infrastructure.Migrations;
using TheBuilder.AppTranslations.Persistence;

namespace TheBuilder.AppTranslations.Migrations;

internal sealed class InstallTranslationSchema(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        CreateIfMissing<SourceRow>(Constants.Tables.Sources);
        CreateIfMissing<MessageRow>(Constants.Tables.Messages);
        CreateIfMissing<OverrideRow>(Constants.Tables.Overrides);
        CreateIfMissing<SyncRow>(Constants.Tables.Syncs);

        Execute.Sql($"CREATE UNIQUE INDEX IX_{Constants.Tables.Messages}_Identity ON {Constants.Tables.Messages} (SourceId, Namespace, [Key], Locale)").Do();
        Execute.Sql($"CREATE UNIQUE INDEX IX_{Constants.Tables.Sources}_Alias ON {Constants.Tables.Sources} (Alias)").Do();
        return Task.CompletedTask;
    }

    private void CreateIfMissing<T>(string tableName)
    {
        if (!TableExists(tableName))
            Create.Table<T>().Do();
    }
}
