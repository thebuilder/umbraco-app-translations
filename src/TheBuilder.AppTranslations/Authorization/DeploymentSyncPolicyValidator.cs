using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using TheBuilder.AppTranslations.Configuration;

namespace TheBuilder.AppTranslations.Authorization;

/// <summary>
/// Stops the site at startup when <see cref="DeploymentSyncOptions.AuthorizationPolicy"/> cannot
/// work, rather than letting the first deployment find out with a 503.
/// </summary>
internal sealed class DeploymentSyncPolicyValidator(
    IAuthorizationPolicyProvider policies,
    IAuthenticationSchemeProvider schemes) : IValidateOptions<DeploymentSyncOptions>
{
    private const string Setting = DeploymentSyncOptions.AuthorizationPolicyConfigurationName;

    public ValidateOptionsResult Validate(string? name, DeploymentSyncOptions options)
    {
        var policyName = options.AuthorizationPolicy;
        if (string.IsNullOrWhiteSpace(policyName))
            return ValidateOptionsResult.Success;

        // Options validation is synchronous. The built-in providers answer from what was registered
        // at startup, so these complete without waiting.
        var policy = policies.GetPolicyAsync(policyName).GetAwaiter().GetResult();
        if (policy is null)
            return ValidateOptionsResult.Fail($"{Setting} names the authorization policy '{policyName}', which is not registered.");

        // Without its own schemes a policy is checked against whoever the site's default scheme signs
        // in, often from a cookie, and a cross-site form could then start a sync.
        if (policy.AuthenticationSchemes.Count == 0)
            return ValidateOptionsResult.Fail($"{Setting} names the authorization policy '{policyName}', which must name the authentication schemes it reads.");

        var missing = policy.AuthenticationSchemes
            .Where(scheme => schemes.GetSchemeAsync(scheme).GetAwaiter().GetResult() is null)
            .ToList();
        return missing.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"{Setting} names the authorization policy '{policyName}', which reads the authentication scheme '{string.Join("', '", missing)}', and nothing registers it. Is the package that provides it installed?");
    }
}
