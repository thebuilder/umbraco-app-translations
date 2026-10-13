using TheBuilder.AppTranslations.Core.Messages;

namespace TheBuilder.AppTranslations.Core.Sources;

public sealed record TranslationSourceMessage(
    string Namespace,
    string Key,
    string Locale,
    string Value,
    MessageFormat Format,
    IReadOnlyDictionary<string, string> Arguments,
    string Checksum);
