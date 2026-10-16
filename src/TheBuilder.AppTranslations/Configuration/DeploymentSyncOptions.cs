namespace TheBuilder.AppTranslations.Configuration;

/// <summary>
/// The deployment sync endpoint. It is off until <see cref="ApiKey"/> is set or the site registers
/// the <see cref="Authorization.TranslationPolicies.DeploymentSync"/> policy, and accepts a request
/// that satisfies either.
/// </summary>
public sealed class DeploymentSyncOptions
{
    public const string SectionName = "AppTranslations:DeploymentSync";
    public const string ApiKeyConfigurationName = $"{SectionName}:ApiKey";

    public string? ApiKey { get; set; }
}
