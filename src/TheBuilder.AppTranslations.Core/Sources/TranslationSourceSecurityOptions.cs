namespace TheBuilder.AppTranslations.Core.Sources;

public sealed class TranslationSourceSecurityOptions
{
    public const string SectionName = "TheBuilder:AppTranslations:SourceSecurity";

    public bool AllowPrivateNetworkEndpoints { get; init; }
}
