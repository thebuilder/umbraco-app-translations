using NPoco;
using TheBuilder.AppTranslations.Core.Messages;
using TheBuilder.AppTranslations.Core.Output;
using TheBuilder.AppTranslations.Core.Persistence;

namespace TheBuilder.AppTranslations.Persistence;

internal static class TranslationSql
{
    public const string MessageSelect = $"""
        SELECT m.*,
               o.Value AS OverrideValue,
               o.SourceChecksumAtEdit,
               o.Version AS OverrideVersion,
               o.CreatedAt AS OverrideCreatedAt,
               o.CreatedBy AS OverrideCreatedBy,
               o.UpdatedAt AS OverrideUpdatedAt,
               o.UpdatedBy AS OverrideUpdatedBy
        FROM {Constants.Tables.Messages} m
        LEFT JOIN {Constants.Tables.Overrides} o ON o.MessageId = m.Id
        """;

    public static Sql Messages(MessageQuery query)
    {
        var sql = new Sql(MessageSelect + " WHERE 1 = 1");
        if (!string.IsNullOrWhiteSpace(query.Locale)) sql.Append("AND m.Locale = @0", query.Locale);
        if (!string.IsNullOrWhiteSpace(query.Namespace)) sql.Append("AND m.Namespace = @0", query.Namespace);
        if (!string.IsNullOrWhiteSpace(query.Query))
            sql.Append(
                $"AND (m.[Key] LIKE @0{SqlLikePattern.EscapeClause} OR m.DefaultValue LIKE @0{SqlLikePattern.EscapeClause} OR o.Value LIKE @0{SqlLikePattern.EscapeClause})",
                SqlLikePattern.Contains(query.Query));
        if (query.Status is not MessageStatusFilter.Missing)
            sql.Append("AND m.State <> @0", TranslationMessageState.Missing.ToString());
        switch (query.Status)
        {
            case MessageStatusFilter.Default: sql.Append("AND o.MessageId IS NULL"); break;
            case MessageStatusFilter.Overridden: sql.Append("AND o.MessageId IS NOT NULL"); break;
            case MessageStatusFilter.NeedsReview: sql.Append("AND o.MessageId IS NOT NULL AND o.SourceChecksumAtEdit <> m.DefaultChecksum"); break;
            case MessageStatusFilter.Missing: sql.Append("AND m.State = @0 AND o.MessageId IS NOT NULL", TranslationMessageState.Missing.ToString()); break;
        }
        return sql.Append("ORDER BY m.Namespace, m.[Key], m.Locale");
    }

    /// <summary>The delivery API's messages for a locale, from the namespaces named, or from every namespace when there are none.</summary>
    public static Sql OutputMessages(string locale, IReadOnlyCollection<string> namespaces)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);
        ArgumentNullException.ThrowIfNull(namespaces);
        foreach (var messageNamespace in namespaces)
            ArgumentException.ThrowIfNullOrWhiteSpace(messageNamespace, nameof(namespaces));

        var sql = new Sql(MessageSelect + " WHERE m.Locale = @0 AND m.State <> @1", locale, TranslationMessageState.Missing.ToString());
        if (namespaces.Count > 0)
            sql.Append("AND m.Namespace IN (@namespaces)", new { namespaces });
        return sql.Append("ORDER BY m.Namespace, m.[Key]");
    }
}
