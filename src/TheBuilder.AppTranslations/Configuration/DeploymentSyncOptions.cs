namespace TheBuilder.AppTranslations.Configuration;

public sealed class DeploymentSyncOptions
{
    public const string SectionName = "AppTranslations:DeploymentSync";
    public const string ApiKeyConfigurationName = $"{SectionName}:ApiKey";

    public string? ApiKey { get; set; }
}
