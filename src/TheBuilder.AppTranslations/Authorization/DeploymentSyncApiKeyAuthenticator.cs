using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using TheBuilder.AppTranslations.Configuration;

namespace TheBuilder.AppTranslations.Authorization;

public sealed class DeploymentSyncApiKeyAuthenticator(IOptions<DeploymentSyncOptions> options)
{
    internal DeploymentSyncAuthenticationResult Authenticate(string? authorizationHeader)
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
