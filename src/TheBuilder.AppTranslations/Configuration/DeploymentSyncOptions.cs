namespace TheBuilder.AppTranslations.Configuration;

/// <summary>
/// The deployment sync endpoint. It is off until <see cref="ApiKey"/> or
/// <see cref="AuthorizationPolicy"/> is set, and accepts a request that satisfies either.
/// </summary>
public sealed class DeploymentSyncOptions
{
    public const string SectionName = "TheBuilder:AppTranslations:DeploymentSync";
    public const string ApiKeyConfigurationName = $"{SectionName}:ApiKey";
    public const string AuthorizationPolicyConfigurationName = $"{SectionName}:AuthorizationPolicy";

    public string? ApiKey { get; set; }

    /// <summary>
    /// The name of an ASP.NET Core authorization policy that may start a sync, such as one that
    /// checks the OIDC token a deployment pipeline sends. The policy must name the authentication
    /// schemes it reads, so a browser's cookie never satisfies it.
    /// </summary>
    public string? AuthorizationPolicy { get; set; }
}
