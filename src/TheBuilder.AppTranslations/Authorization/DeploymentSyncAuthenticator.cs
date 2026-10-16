using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TheBuilder.AppTranslations.Configuration;

namespace TheBuilder.AppTranslations.Authorization;

/// <summary>
/// Checks who may call the deployment sync endpoint: a bearer token matching
/// <see cref="DeploymentSyncOptions.ApiKey"/>, or a request that satisfies the
/// <see cref="TranslationPolicies.DeploymentSync"/> policy when the site registers one.
/// </summary>
/// <remarks>
/// Public only because the public <see cref="AutomationApi.DeploymentSyncController"/> takes it in
/// its constructor, and ASP.NET only calls public constructors. Its members are internal, so it is
/// not an extension point: configure the key, or register the policy, instead.
/// </remarks>
public interface IDeploymentSyncAuthenticator
{
    internal Task<DeploymentSyncAuthenticationResult> AuthenticateAsync(HttpContext context);
}

internal sealed class DeploymentSyncAuthenticator(
    IOptions<DeploymentSyncOptions> options,
    IOptions<AuthorizationOptions> authorization) : IDeploymentSyncAuthenticator
{
    public async Task<DeploymentSyncAuthenticationResult> AuthenticateAsync(HttpContext context)
    {
        var apiKey = options.Value.ApiKey;
        var acceptsApiKey = !string.IsNullOrWhiteSpace(apiKey);
        if (acceptsApiKey && ApiKey.Matches(apiKey!, ApiKey.FromBearerHeader(context.Request.Headers.Authorization)))
            return DeploymentSyncAuthenticationResult.Authorized;

        // From the options DeploymentSyncPolicyValidator checked at startup, not from
        // IAuthorizationPolicyProvider, which a site can replace with one that answers differently.
        var policy = authorization.Value.GetPolicy(TranslationPolicies.DeploymentSync);
        if (policy is null)
            return acceptsApiKey ? DeploymentSyncAuthenticationResult.AskForKey : DeploymentSyncAuthenticationResult.Disabled;

        // What the authorization middleware does for [Authorize], which the endpoint cannot carry:
        // it is anonymous so that the key check above can run.
        var evaluator = context.RequestServices.GetRequiredService<IPolicyEvaluator>();
        var authentication = await evaluator.AuthenticateAsync(policy, context);
        var result = await evaluator.AuthorizeAsync(policy, authentication, context, resource: null);
        if (result.Succeeded)
            return DeploymentSyncAuthenticationResult.Authorized;
        if (result.Forbidden)
            return DeploymentSyncAuthenticationResult.Forbidden;
        return acceptsApiKey ? DeploymentSyncAuthenticationResult.AskForKey : DeploymentSyncAuthenticationResult.Unauthorized;
    }
}

internal enum DeploymentSyncAuthenticationResult
{
    /// <summary>No key is configured and no policy is registered.</summary>
    Disabled,

    /// <summary>Refused, and only the policy could have let the request in.</summary>
    Unauthorized,

    /// <summary>Refused, and a key would have been accepted, so the response asks for one.</summary>
    AskForKey,

    /// <summary>Authenticated, but the policy turned the caller down.</summary>
    Forbidden,

    Authorized,
}
