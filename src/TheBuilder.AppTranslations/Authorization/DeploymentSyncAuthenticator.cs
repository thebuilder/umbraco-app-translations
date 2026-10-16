using System.Net.Http.Headers;
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
    /// <summary>Whether a key is configured, so a refused request is told to send one.</summary>
    internal bool AcceptsApiKey { get; }

    internal Task<DeploymentSyncAuthenticationResult> AuthenticateAsync(HttpContext context);
}

internal sealed class DeploymentSyncAuthenticator(IOptions<DeploymentSyncOptions> options) : IDeploymentSyncAuthenticator
{
    public bool AcceptsApiKey => !string.IsNullOrWhiteSpace(options.Value.ApiKey);

    public async Task<DeploymentSyncAuthenticationResult> AuthenticateAsync(HttpContext context)
    {
        var settings = options.Value;
        if (AcceptsApiKey && ApiKey.Matches(settings.ApiKey!, BearerToken(context.Request.Headers.Authorization)))
            return DeploymentSyncAuthenticationResult.Authorized;

        var policy = await context.RequestServices.GetRequiredService<IAuthorizationPolicyProvider>().GetPolicyAsync(TranslationPolicies.DeploymentSync);
        if (policy is null)
            return AcceptsApiKey ? DeploymentSyncAuthenticationResult.Unauthorized : DeploymentSyncAuthenticationResult.Disabled;

        // What the authorization middleware does for [Authorize], which the endpoint cannot carry:
        // it is anonymous so that the key check above can run.
        var evaluator = context.RequestServices.GetRequiredService<IPolicyEvaluator>();
        var authentication = await evaluator.AuthenticateAsync(policy, context);
        var authorization = await evaluator.AuthorizeAsync(policy, authentication, context, resource: null);
        if (authorization.Succeeded)
            return DeploymentSyncAuthenticationResult.Authorized;
        return authorization.Forbidden
            ? DeploymentSyncAuthenticationResult.Forbidden
            : DeploymentSyncAuthenticationResult.Unauthorized;
    }

    private static string? BearerToken(string? authorizationHeader) =>
        AuthenticationHeaderValue.TryParse(authorizationHeader, out var authorization) &&
        authorization.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
            ? authorization.Parameter
            : null;
}

internal enum DeploymentSyncAuthenticationResult
{
    Disabled,
    Unauthorized,
    Forbidden,
    Authorized,
}
