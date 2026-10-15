namespace TheBuilder.AppTranslations;

public static class Constants
{
    public const string PackageName = "TheBuilder.AppTranslations";
    public const string SectionAlias = "TheBuilder.AppTranslations.Section";
    public const string ApiName = "thebuilder-app-translations";
    public const string AutomationApiName = "thebuilder-app-translations-automation";
    public const string ApiRoot = "management/api/v{version:apiVersion}/app-translations";
    public const string DeliveryApiRoot = "umbraco/delivery/api/v{version:apiVersion}/app-translations";
    public const string AutomationApiRoot = "umbraco/app-translations/api/v{version:apiVersion}";

    internal static class Tables
    {
        public const string Sources = "theBuilderAppTranslationSource";
        public const string Messages = "theBuilderAppTranslationMessage";
        public const string Overrides = "theBuilderAppTranslationOverride";
        public const string Syncs = "theBuilderAppTranslationSync";
    }
}
