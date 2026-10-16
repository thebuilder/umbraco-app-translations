namespace TheBuilder.AppTranslations.Authorization;

public static class TranslationPolicies
{
    public const string View = "TheBuilder.AppTranslations.View";
    public const string Edit = "TheBuilder.AppTranslations.Edit";
    public const string ManageSources = "TheBuilder.AppTranslations.ManageSources";
    public const string Sync = "TheBuilder.AppTranslations.Sync";

    /// <summary>
    /// Not registered by the package. A site registers it to let deployment sync accept something
    /// other than the key, such as the OIDC token its pipeline sends. It must name the
    /// authentication schemes it reads, so a browser's cookie never satisfies it.
    /// </summary>
    public const string DeploymentSync = "TheBuilder.AppTranslations.DeploymentSync";
}
