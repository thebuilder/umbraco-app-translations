namespace TheBuilder.AppTranslations.Authorization;

public static class TranslationPolicies
{
    public const string View = "TheBuilder.AppTranslations.View";
    public const string Edit = "TheBuilder.AppTranslations.Edit";
    public const string ManageSources = "TheBuilder.AppTranslations.ManageSources";
    public const string Sync = "TheBuilder.AppTranslations.Sync";

    /// <summary>
    /// Not registered by the package. A site registers it to let deployment sync accept something
    /// other than the key, such as the OIDC token its pipeline sends. The site won't start if the
    /// policy names no authentication scheme, or a cookie scheme, because a browser could then
    /// start a sync. The policy alone decides who gets in, so it must not let anonymous requests
    /// through.
    /// </summary>
    public const string DeploymentSync = "TheBuilder.AppTranslations.DeploymentSync";
}
