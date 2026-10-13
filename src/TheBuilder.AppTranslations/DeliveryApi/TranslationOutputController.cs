using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using TheBuilder.AppTranslations.Authorization;
using TheBuilder.AppTranslations.Configuration;
using TheBuilder.AppTranslations.Core.Output;
using TheBuilder.AppTranslations.Core.Persistence;

namespace TheBuilder.AppTranslations.DeliveryApi;

[ApiController]
[ApiVersion("1.0")]
[Route(Constants.DeliveryApiRoot)]
public sealed class TranslationOutputController(
    ITranslationMessageRepository store,
    ITranslationOutputSerializer serializer,
    DeliveryApiKeyAuthenticator authenticator,
    DeliveryResponseCache cache,
    IOptions<DeliveryOptions> options) : ControllerBase
{
    [HttpGet("overrides")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> GetOverrides(
        [FromQuery, Required] string? locale,
        [FromQuery(Name = "namespace"), Required] string? messageNamespace,
        string format = "next-intl",
        CancellationToken cancellationToken = default) =>
        GetOutput(locale, messageNamespace, format, TranslationOutputMode.Overrides, cancellationToken);

    [HttpGet("all")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> GetAll(
        [FromQuery, Required] string? locale,
        [FromQuery(Name = "namespace"), Required] string? messageNamespace,
        string format = "next-intl",
        CancellationToken cancellationToken = default) =>
        GetOutput(locale, messageNamespace, format, TranslationOutputMode.All, cancellationToken);

    private async Task<IActionResult> GetOutput(
        string? locale,
        string? messageNamespace,
        string format,
        TranslationOutputMode mode,
        CancellationToken cancellationToken)
    {
        var access = authenticator.Authenticate(Request.Headers[DeliveryApiKeyAuthenticator.HeaderName], Request.Headers.Authorization);
        if (access is DeliveryAuthenticationResult.Unauthorized)
        {
            Response.Headers.WWWAuthenticate = "Bearer";
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(locale))
            return BadRequest("The locale query parameter is required.");
        if (string.IsNullOrWhiteSpace(messageNamespace))
            return BadRequest("The namespace query parameter is required.");
        if (!TranslationOutputFormats.TryParse(format, out var outputFormat))
            return BadRequest($"Unsupported translation output format '{format}'.");

        var key = new DeliveryResponseKey(mode, outputFormat, locale, messageNamespace);
        if (!cache.TryGet(key, out var response))
        {
            var generation = cache.Generation;
            var namespaceMessages = await store.GetOutputMessagesAsync(locale, messageNamespace, cancellationToken);
            var incompatible = namespaceMessages.FirstOrDefault(message => !TranslationOutputFormats.Supports(outputFormat, message.Message.Format));
            if (incompatible is not null)
                return Conflict($"Namespace '{messageNamespace}' contains {incompatible.Message.Format} messages that are incompatible with output format '{format}'. Use a separate namespace for each message syntax.");
            string json;
            try
            {
                json = serializer.Serialize(namespaceMessages, mode);
            }
            catch (TranslationOutputCollisionException exception)
            {
                return Conflict(exception.Message);
            }
            response = new CachedDeliveryResponse(json, $"\"{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant()}\"");
            // Only responses with messages in them, asked for by the spelling the messages are stored
            // under. Anyone can ask for a locale or namespace that does not exist, and on a
            // case-insensitive database for every casing of one that does; keeping each of those
            // would let them fill the server's memory. Other spellings are still served, just
            // built each time.
            if (namespaceMessages.Count > 0 &&
                string.Equals(namespaceMessages[0].Message.Identity.Locale, locale, StringComparison.Ordinal) &&
                string.Equals(namespaceMessages[0].Message.Identity.Namespace, messageNamespace, StringComparison.Ordinal))
                cache.Store(key, response, generation);
        }

        // On the 304 too: a revalidated response carries the same validator and caching rules.
        Response.Headers.ETag = response.ETag;
        Response.Headers.CacheControl = CacheControl(access, options.Value.MaxAgeSeconds);
        if (Matches(Request.Headers.IfNoneMatch, response.ETag))
            return StatusCode(StatusCodes.Status304NotModified);
        return Content(response.Json, "application/json", Encoding.UTF8);
    }

    /// <summary>
    /// Whether <c>If-None-Match</c> names this response, compared the way HTTP says to: weakly, so a
    /// proxy that compressed the response and weakened its ETag to <c>W/"..."</c> still gets a 304,
    /// against every tag in the list, and with <c>*</c> matching anything.
    /// </summary>
    internal static bool Matches(StringValues ifNoneMatch, string etag)
    {
        if (!EntityTagHeaderValue.TryParseList(ifNoneMatch, out var tags))
            return false;
        var current = new EntityTagHeaderValue(etag);
        return tags.Any(tag => tag.Equals(EntityTagHeaderValue.Any) || tag.Compare(current, useStrongComparison: false));
    }

    /// <summary>
    /// A public response may sit in a shared cache. One that needed a key must stay with the client
    /// that sent it, or a CDN would hand it to anyone who asked. With no lifetime, clients keep the
    /// response but check it each time, which the ETag and Umbraco's own copy make cheap.
    /// </summary>
    internal static string CacheControl(DeliveryAuthenticationResult access, int maxAgeSeconds) =>
        $"{(access is DeliveryAuthenticationResult.Public ? "public" : "private")},{(maxAgeSeconds == 0 ? "no-cache" : $"max-age={maxAgeSeconds}")}";
}
