using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace TheBuilder.AppTranslations.Authorization;

/// <summary>
/// Stops the site at startup when the <see cref="TranslationPolicies.DeploymentSync"/> policy it
/// registered cannot work, or would let a browser start a sync, rather than letting the first
/// deployment find out.
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

        foreach (var schemeName in policy.AuthenticationSchemes)
        {
            // Options validation is synchronous. The built-in provider answers from what was
            // registered at startup, so this completes without waiting.
            var scheme = schemes.GetSchemeAsync(schemeName).GetAwaiter().GetResult();
            if (scheme is null)
                return ValidateOptionsResult.Fail(
                    $"The {Policy} authorization policy reads the authentication scheme '{schemeName}', and nothing registers it. Is the package that provides it installed?");

            // A browser sends its cookies with a cross-site form too, so a cookie scheme named
            // outright is as unsafe as none.
            if (scheme.HandlerType.IsAssignableTo(typeof(CookieAuthenticationHandler)))
                return ValidateOptionsResult.Fail(
                    $"The {Policy} authorization policy reads the cookie scheme '{schemeName}'. A browser sends cookies on its own, so use a scheme that reads a token the caller sends.");
        }

        return ValidateOptionsResult.Success;
    }
}
