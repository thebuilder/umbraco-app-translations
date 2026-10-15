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
using TheBuilder.AppTranslations.Core.Messages;
using TheBuilder.AppTranslations.Core.Output;
using TheBuilder.AppTranslations.Core.Persistence;

namespace TheBuilder.AppTranslations.DeliveryApi;

[ApiController]
[ApiVersion("1.0")]
[Route(Constants.DeliveryApiRoot)]
public sealed class TranslationOutputController(
    ITranslationMessageRepository store,
    ITranslationOutputSerializer serializer,
    IDeliveryApiKeyAuthenticator authenticator,
    IDeliveryResponseCache cache,
    IOptions<DeliveryOptions> options) : ControllerBase
{
    internal const int MaximumNamespacesPerRequest = 20;

    // One namespace returns its messages. Several, or none for every namespace, return each
    // namespace under its own name. Not [Required]: the attribute answers with the C# parameter
    // name, which is not the query parameter's. Without a format, the messages are served in
    // whatever syntax they are written in; with one, anything else is refused.
    [HttpGet("overrides")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> GetOverrides(
        [FromQuery, Required] string? locale,
        [FromQuery(Name = "namespace")] string[]? namespaces,
        string? format = null,
        CancellationToken cancellationToken = default) =>
        GetOutput(locale, namespaces, format, TranslationOutputMode.Overrides, cancellationToken);

    [HttpGet("all")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> GetAll(
        [FromQuery, Required] string? locale,
        [FromQuery(Name = "namespace")] string[]? namespaces,
        string? format = null,
        CancellationToken cancellationToken = default) =>
        GetOutput(locale, namespaces, format, TranslationOutputMode.All, cancellationToken);

    private async Task<IActionResult> GetOutput(
        string? locale,
        string[]? requestedNamespaces,
        string? format,
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
        TranslationOutputFormat? outputFormat = null;
        if (!string.IsNullOrWhiteSpace(format))
        {
            if (!TranslationOutputFormats.TryParse(format, out var parsed))
                return BadRequest($"Unsupported translation output format '{format}'. Use icu or i18next, or leave format out.");
            outputFormat = parsed;
        }
        var namespaces = DeliveryNamespaces.From(requestedNamespaces);
        if (namespaces.Names.Count > MaximumNamespacesPerRequest)
            return BadRequest($"Ask for at most {MaximumNamespacesPerRequest} namespaces at once, or leave namespace out for every namespace.");

        var key = new DeliveryResponseKey(mode, outputFormat, locale, namespaces);
        if (!cache.TryGet(key, out var response))
        {
            var generation = cache.Generation;
            var namespaceMessages = await store.GetOutputMessagesAsync(locale, namespaces.Names, cancellationToken);
            // namespace=a,b names one namespace called "a,b". When there is none, the caller
            // almost certainly meant two, and an empty 200 would hide that.
            var commaList = namespaces.Names.FirstOrDefault(name =>
                name.Contains(',', StringComparison.Ordinal) &&
                !namespaceMessages.Any(message => string.Equals(message.Message.Identity.Namespace, name, StringComparison.OrdinalIgnoreCase)));
            if (commaList is not null)
                return BadRequest($"No namespace is called '{commaList}'. To ask for several namespaces, repeat the parameter: namespace=a&namespace=b.");
            if (SyntaxConflict(namespaceMessages, outputFormat, namespaces) is { } conflict)
                return Conflict(conflict);
            string json;
            try
            {
                json = DeliveryOutput.Serialize(serializer, namespaceMessages, mode, namespaces);
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
                StoredAsAskedFor(namespaces, namespaceMessages))
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
    /// Why the messages cannot be served together, or null when they can. With a format, every
    /// message has to be readable in it. Without one, they only have to share a syntax: an
    /// application reads one of ICU and i18next, never both. Plain text reads as either.
    /// </summary>
    internal static string? SyntaxConflict(
        IReadOnlyList<TranslationMessageView> messages,
        TranslationOutputFormat? format,
        DeliveryNamespaces namespaces)
    {
        if (format is { } required)
        {
            var unreadable = messages.FirstOrDefault(message => !TranslationOutputFormats.Supports(required, message.Message.Format));
            if (unreadable is null)
                return null;
            var unreadableNamespace = unreadable.Message.Identity.Namespace;
            // A namespace in one syntax only needs the other format. One that mixes them cannot be
            // served in either until it is split.
            var mixes = messages.Any(message =>
                string.Equals(message.Message.Identity.Namespace, unreadableNamespace, StringComparison.Ordinal) &&
                message.Message.Format is not MessageFormat.PlainText &&
                message.Message.Format != unreadable.Message.Format);
            var advice = mixes
                ? "Use a separate namespace for each message syntax."
                : $"Ask for it with format={FormatName(unreadable.Message.Format)}, or leave format out.";
            return $"Namespace '{unreadableNamespace}' has {SyntaxName(unreadable.Message.Format)} messages, which format={FormatName(required)} does not read. {advice}";
        }

        var first = messages.FirstOrDefault(message => message.Message.Format is not MessageFormat.PlainText);
        var other = first is null
            ? null
            : messages.FirstOrDefault(message => message.Message.Format is not MessageFormat.PlainText && message.Message.Format != first.Message.Format);
        if (other is null)
            return null;
        return string.Equals(first!.Message.Identity.Namespace, other.Message.Identity.Namespace, StringComparison.Ordinal)
            ? $"Namespace '{other.Message.Identity.Namespace}' mixes {SyntaxName(first.Message.Format)} and {SyntaxName(other.Message.Format)} messages. Use a separate namespace for each message syntax."
            : $"Namespace '{first.Message.Identity.Namespace}' has {SyntaxName(first.Message.Format)} messages and namespace '{other.Message.Identity.Namespace}' has {SyntaxName(other.Message.Format)} messages. Ask for them separately.";
    }

    private static string FormatName(TranslationOutputFormat format) =>
        format is TranslationOutputFormat.Icu ? "icu" : "i18next";

    private static string FormatName(MessageFormat format) =>
        format is MessageFormat.I18NextV4 ? "i18next" : "icu";

    private static string SyntaxName(MessageFormat format) => format switch
    {
        MessageFormat.Icu => "ICU",
        MessageFormat.I18NextV4 => "i18next",
        _ => "plain text",
    };

    /// <summary>Whether every namespace the request named has messages stored under that exact spelling.</summary>
    private static bool StoredAsAskedFor(DeliveryNamespaces namespaces, IReadOnlyList<TranslationMessageView> messages)
    {
        if (namespaces.Names.Count == 0)
            return true;
        var stored = messages.Select(message => message.Message.Identity.Namespace).ToHashSet(StringComparer.Ordinal);
        return namespaces.Names.All(stored.Contains);
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
