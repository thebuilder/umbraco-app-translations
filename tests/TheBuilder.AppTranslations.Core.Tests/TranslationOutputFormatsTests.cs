using TheBuilder.AppTranslations.Core.Messages;
using TheBuilder.AppTranslations.Core.Output;
using TheBuilder.AppTranslations.Core.Persistence;

namespace TheBuilder.AppTranslations.Core.Tests;

public sealed class TranslationOutputFormatsTests
{
    [Fact]
    public void Creates_typed_endpoints_and_conflicts_from_message_format_facts()
    {
        var data = new TranslationFacetData(
        [
            new("da", "website", [MessageFormat.Icu, MessageFormat.PlainText]),
            new("en", "app", [MessageFormat.I18NextV4, MessageFormat.PlainText]),
            new("en", "mixed", [MessageFormat.Icu, MessageFormat.I18NextV4]),
        ],
        new Dictionary<MessageStatusFilter, int>(),
        [new TranslationLocaleUsage("da", 2, 1, 0), new TranslationLocaleUsage("en", 3, 0, 0)],
        TotalKeys: 4);

        var facets = TranslationOutputFormats.CreateFacets(data);

        Assert.Contains(new TranslationOutputEndpoint("da", "website", TranslationOutputFormat.Icu), facets.OutputEndpoints);
        Assert.Contains(new TranslationOutputEndpoint("en", "app", TranslationOutputFormat.I18Next), facets.OutputEndpoints);
        Assert.DoesNotContain(facets.OutputEndpoints, endpoint => endpoint.Namespace == "mixed");
        Assert.Contains(facets.OutputConflicts, conflict => conflict.Namespace == "mixed");
    }
}
