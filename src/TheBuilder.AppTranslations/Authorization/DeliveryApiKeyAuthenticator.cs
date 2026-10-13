using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using TheBuilder.AppTranslations.Configuration;

namespace TheBuilder.AppTranslations.Authorization;

/// <summary>
/// Checks the key the delivery endpoints ask for once <see cref="DeliveryOptions.ApiKey"/> is set.
/// It reads an <c>Api-Key</c> header, as Umbraco's own Delivery API does, or a bearer token, as the
/// package's deployment sync does, so a client written for either works.
/// </summary>
public sealed class DeliveryApiKeyAuthenticator(IOptions<DeliveryOptions> options)
{
    public const string HeaderName = "Api-Key";

    public bool KeyRequired => !string.IsNullOrWhiteSpace(options.Value.ApiKey);

    internal DeliveryAuthenticationResult Authenticate(string? apiKeyHeader, string? authorizationHeader)
    {
        var configuredKey = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(configuredKey))
            return DeliveryAuthenticationResult.Public;

        var suppliedKey = !string.IsNullOrEmpty(apiKeyHeader)
            ? apiKeyHeader
            : AuthenticationHeaderValue.TryParse(authorizationHeader, out var authorization) &&
              authorization.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
                ? authorization.Parameter
                : null;
        return ApiKey.Matches(configuredKey, suppliedKey)
            ? DeliveryAuthenticationResult.Authorized
            : DeliveryAuthenticationResult.Unauthorized;
    }
}

internal enum DeliveryAuthenticationResult
{
    Public,
    Unauthorized,
    Authorized,
}
