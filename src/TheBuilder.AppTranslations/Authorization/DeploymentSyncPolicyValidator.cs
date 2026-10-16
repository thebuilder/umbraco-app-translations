using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace TheBuilder.AppTranslations.Authorization;

/// <summary>
/// Stops the site at startup when the <see cref="TranslationPolicies.DeploymentSync"/> policy it
/// registered cannot work, rather than letting the first deployment find out.
/// </summary>
internal sealed class DeploymentSyncPolicyValidator(IAuthenticationSchemeProvider schemes) : IValidateOptions<AuthorizationOptions>
{
    private const string Policy = TranslationPolicies.DeploymentSync;

    public ValidateOptionsResult Validate(string? name, AuthorizationOptions options)
    {
        var policy = options.GetPolicy(Policy);
        if (policy is null)
            return ValidateOptionsResult.Success;

        // Without its own schemes a policy is checked against whoever the site's default scheme signs
        // in, often from a cookie, and a cross-site form could then start a sync.
        if (policy.AuthenticationSchemes.Count == 0)
            return ValidateOptionsResult.Fail($"The {Policy} authorization policy must name the authentication schemes it reads.");

        // Options validation is synchronous. The built-in provider answers from what was registered
        // at startup, so this completes without waiting.
        var missing = policy.AuthenticationSchemes
            .Where(scheme => schemes.GetSchemeAsync(scheme).GetAwaiter().GetResult() is null)
            .ToList();
        return missing.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"The {Policy} authorization policy reads the authentication scheme '{string.Join("', '", missing)}', and nothing registers it. Is the package that provides it installed?");
    }
}
