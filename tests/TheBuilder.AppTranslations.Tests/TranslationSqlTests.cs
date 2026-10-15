using TheBuilder.AppTranslations.Core.Persistence;
using TheBuilder.AppTranslations.Core.Output;
using TheBuilder.AppTranslations.Persistence;

namespace TheBuilder.AppTranslations.Tests;

public sealed class TranslationSqlTests
{
    [Theory]
    [InlineData(MessageStatusFilter.All)]
    [InlineData(MessageStatusFilter.Default)]
    [InlineData(MessageStatusFilter.Overridden)]
    [InlineData(MessageStatusFilter.NeedsReview)]
    public void Normal_views_exclude_missing_messages(MessageStatusFilter status)
    {
        var sql = TranslationSql.Messages(new MessageQuery(Status: status));

        Assert.Contains("m.State <>", sql.SQL, StringComparison.Ordinal);
        Assert.Contains(sql.Arguments, argument => Equals(argument, "Missing"));
    }

    [Fact]
    public void Missing_view_includes_only_missing_messages()
    {
        var sql = TranslationSql.Messages(new MessageQuery(Status: MessageStatusFilter.Missing));

        Assert.DoesNotContain("m.State <>", sql.SQL, StringComparison.Ordinal);
        Assert.Contains("m.State =", sql.SQL, StringComparison.Ordinal);
        Assert.Contains("o.MessageId IS NOT NULL", sql.SQL, StringComparison.Ordinal);
        Assert.Contains(sql.Arguments, argument => Equals(argument, "Missing"));
    }

    [Fact]
    public void Output_snapshot_includes_overridden_and_inherited_messages_but_excludes_missing_messages()
    {
        var sql = TranslationSql.OutputMessages("da", ["website"]);

        Assert.DoesNotContain("o.MessageId IS NOT NULL", sql.SQL, StringComparison.Ordinal);
        Assert.Contains("m.State <>", sql.SQL, StringComparison.Ordinal);
        Assert.Contains(sql.Arguments, argument => Equals(argument, "Missing"));
    }

    [Fact]
    public void Output_for_a_namespace_filters_on_it()
    {
        var sql = TranslationSql.OutputMessages("da", ["website"]);

        Assert.Contains("m.Namespace IN (", sql.SQL, StringComparison.Ordinal);
        Assert.Contains(sql.Arguments, argument => Equals(argument, "website"));
    }

    [Fact]
    public void Output_for_several_namespaces_filters_on_each()
    {
        var sql = TranslationSql.OutputMessages("da", ["checkout", "website"]);

        Assert.Contains(sql.Arguments, argument => Equals(argument, "checkout"));
        Assert.Contains(sql.Arguments, argument => Equals(argument, "website"));
    }

    [Fact]
    public void Output_without_a_namespace_reads_every_namespace()
    {
        var sql = TranslationSql.OutputMessages("da", []);

        Assert.DoesNotContain("m.Namespace", sql.SQL.Replace("ORDER BY m.Namespace", "", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains(sql.Arguments, argument => Equals(argument, "Missing"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Output_refuses_a_blank_namespace(string messageNamespace)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            TranslationSql.OutputMessages("da", ["website", messageNamespace]));
    }
}
