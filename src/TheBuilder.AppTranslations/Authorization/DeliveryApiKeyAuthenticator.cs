using Microsoft.Extensions.Options;
using TheBuilder.AppTranslations.Configuration;

namespace TheBuilder.AppTranslations.Authorization;

/// <summary>Checks the key the delivery endpoints ask for, against <see cref="DeliveryOptions.ApiKey"/>.</summary>
/// <remarks>
/// Public only because the public <see cref="DeliveryApi.TranslationOutputController"/> and
/// <see cref="ManagementApi.MessagesController"/> take it in their constructors, and ASP.NET only
/// calls public constructors. Its members are internal, so it is not an extension point: configure
/// <see cref="DeliveryOptions.ApiKey"/> instead.
/// </remarks>
public interface IDeliveryApiKeyAuthenticator
{
    /// <summary>Whether a key is configured, so the delivery endpoints refuse requests without it.</summary>
    internal bool KeyRequired { get; }

    internal DeliveryAuthenticationResult Authenticate(string? apiKeyHeader, string? authorizationHeader);
}

/// <summary>
/// Checks the key the delivery endpoints ask for once <see cref="DeliveryOptions.ApiKey"/> is set.
/// It reads an <c>Api-Key</c> header, as Umbraco's own Delivery API does, or a bearer token, as the
/// package's deployment sync does, so a client written for either works.
/// </summary>
internal sealed class DeliveryApiKeyAuthenticator(IOptions<DeliveryOptions> options) : IDeliveryApiKeyAuthenticator
{
    public const string HeaderName = "Api-Key";

    public bool KeyRequired => !string.IsNullOrWhiteSpace(options.Value.ApiKey);

    public DeliveryAuthenticationResult Authenticate(string? apiKeyHeader, string? authorizationHeader)
    {
        var configuredKey = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(configuredKey))
            return DeliveryAuthenticationResult.Public;

        var suppliedKey = !string.IsNullOrEmpty(apiKeyHeader)
            ? apiKeyHeader
            : ApiKey.FromBearerHeader(authorizationHeader);
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
