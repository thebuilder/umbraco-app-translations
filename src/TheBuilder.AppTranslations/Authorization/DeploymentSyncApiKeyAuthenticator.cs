using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using TheBuilder.AppTranslations.Configuration;

namespace TheBuilder.AppTranslations.Authorization;

/// <summary>
/// Checks the bearer token the deployment sync endpoint asks for, against
/// <see cref="DeploymentSyncOptions.ApiKey"/>.
/// </summary>
/// <remarks>
/// Public only because the public <see cref="AutomationApi.DeploymentSyncController"/> takes it in
/// its constructor, and ASP.NET only calls public constructors. Its members are internal, so it is
/// not an extension point: configure <see cref="DeploymentSyncOptions.ApiKey"/> instead.
/// </remarks>
public interface IDeploymentSyncApiKeyAuthenticator
{
    internal DeploymentSyncAuthenticationResult Authenticate(string? authorizationHeader);
}

internal sealed class DeploymentSyncApiKeyAuthenticator(IOptions<DeploymentSyncOptions> options) : IDeploymentSyncApiKeyAuthenticator
{
    public DeploymentSyncAuthenticationResult Authenticate(string? authorizationHeader)
    {
        var configuredKey = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(configuredKey))
            return DeploymentSyncAuthenticationResult.Disabled;

        if (!AuthenticationHeaderValue.TryParse(authorizationHeader, out var authorization) ||
            !authorization.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(authorization.Parameter))
            return DeploymentSyncAuthenticationResult.Unauthorized;

        return ApiKey.Matches(configuredKey, authorization.Parameter)
            ? DeploymentSyncAuthenticationResult.Authorized
            : DeploymentSyncAuthenticationResult.Unauthorized;
    }
}

internal enum DeploymentSyncAuthenticationResult
{
    Disabled,
    Unauthorized,
    Authorized,
}
