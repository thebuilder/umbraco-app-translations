using System.Text.Json.Serialization;
using TheBuilder.AppTranslations.Core.Messages;
using TheBuilder.AppTranslations.Core.Persistence;

namespace TheBuilder.AppTranslations.Core.Output;

/// <summary>
/// The message syntax an application can read, named for the syntax rather than for a library:
/// next-intl, react-intl and Lingui all read ICU, and react-i18next reads what i18next does. The
/// JSON is the same either way. A format only makes sure a response holds nothing the application
/// would misread.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TranslationOutputFormat>))]
public enum TranslationOutputFormat
{
    [JsonStringEnumMemberName("icu")]
    Icu,

    [JsonStringEnumMemberName("i18next")]
    I18Next,
}

internal static class TranslationOutputFormats
{
    public static bool TryParse(string value, out TranslationOutputFormat format)
    {
        switch (value.ToLowerInvariant())
        {
            case "icu":
                format = TranslationOutputFormat.Icu;
                return true;
            case "i18next":
                format = TranslationOutputFormat.I18Next;
                return true;
            default:
                format = default;
                return false;
        }
    }

    public static bool Supports(TranslationOutputFormat format, MessageFormat messageFormat) => format switch
    {
        TranslationOutputFormat.Icu => messageFormat is MessageFormat.Icu or MessageFormat.PlainText,
        TranslationOutputFormat.I18Next => messageFormat is MessageFormat.I18NextV4 or MessageFormat.PlainText,
        _ => false,
    };

    public static IReadOnlyList<TranslationOutputFormat> CompatibleWith(IEnumerable<MessageFormat> messageFormats)
    {
        var formats = messageFormats.ToHashSet();
        return Enum.GetValues<TranslationOutputFormat>()
            .Where(outputFormat => formats.All(messageFormat => Supports(outputFormat, messageFormat)))
            .ToArray();
    }

    public static TranslationFacets CreateFacets(TranslationFacetData data)
    {
        var classified = data.OutputGroups
            .Select(group => new
            {
                Group = group,
                Formats = CompatibleWith(group.MessageFormats),
            })
            .ToArray();
        return new(
            data.Locales,
            data.OutputGroups.Select(group => group.Namespace).Distinct().Order().ToArray(),
            classified.SelectMany(item => item.Formats.Select(format =>
                new TranslationOutputEndpoint(item.Group.Locale, item.Group.Namespace, format))).ToArray(),
            classified.Where(item => item.Formats.Count == 0).Select(item =>
                new TranslationOutputConflict(item.Group.Locale, item.Group.Namespace, item.Group.MessageFormats)).ToArray(),
            data.StatusCounts,
            data.TotalKeys);
    }
}
