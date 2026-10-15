using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TheBuilder.AppTranslations.Authorization;
using TheBuilder.AppTranslations.Configuration;
using TheBuilder.AppTranslations.Core.Output;
using TheBuilder.AppTranslations.Core.Persistence;
using TheBuilder.AppTranslations.Core.Messages;
using TheBuilder.AppTranslations.DeliveryApi;

namespace TheBuilder.AppTranslations.Tests;

public sealed class TranslationOutputControllerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Output_without_a_namespace_has_every_namespace_under_its_own_name(string? messageNamespace)
    {
        var repository = new TwoNamespacesRepository();
        var controller = Create(repository);

        var result = await controller.GetAll("da", messageNamespace is null ? null : [messageNamespace], cancellationToken: CancellationToken.None);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Empty(repository.AskedFor!);
        Assert.Equal("{\"checkout\":{\"title\":\"Betaling\"},\"navigation\":{\"title\":\"Menu\"},\"search\":{\"title\":\"S\\u00F8g\"}}", content.Content);
    }

    [Fact]
    public async Task Output_for_several_namespaces_has_each_under_its_own_name()
    {
        var repository = new TwoNamespacesRepository();

        var result = await Create(repository).GetAll("da", ["navigation", "checkout", "navigation", " "], cancellationToken: CancellationToken.None);

        Assert.Equal(["checkout", "navigation"], repository.AskedFor);
        Assert.Equal("{\"checkout\":{\"title\":\"Betaling\"},\"navigation\":{\"title\":\"Menu\"}}", Assert.IsType<ContentResult>(result).Content);
    }

    [Fact]
    public async Task The_same_namespaces_in_another_order_are_the_same_response()
    {
        var repository = new TwoNamespacesRepository();
        var cache = new DeliveryResponseCache(TimeProvider.System);

        var first = Create(repository, cache: cache);
        await first.GetAll("da", ["navigation", "checkout"], cancellationToken: CancellationToken.None);
        var second = Create(repository, cache: cache);
        await second.GetAll("da", ["checkout", "navigation"], cancellationToken: CancellationToken.None);

        Assert.Equal(1, repository.Reads);
        Assert.Equal(first.Response.Headers.ETag.ToString(), second.Response.Headers.ETag.ToString());
    }

    [Fact]
    public async Task Several_namespaces_including_one_that_does_not_exist_are_served_but_not_kept()
    {
        var repository = new TwoNamespacesRepository();
        var cache = new DeliveryResponseCache(TimeProvider.System);

        await Create(repository, cache: cache).GetAll("da", ["navigation", "made-up"], cancellationToken: CancellationToken.None);
        await Create(repository, cache: cache).GetAll("da", ["navigation", "made-up"], cancellationToken: CancellationToken.None);

        Assert.Equal(2, repository.Reads);
    }

    [Fact]
    public async Task Several_namespaces_are_answered_under_the_spelling_asked_for()
    {
        var repository = new TwoNamespacesRepository(caseInsensitive: true);
        var cache = new DeliveryResponseCache(TimeProvider.System);

        var result = await Create(repository, cache: cache).GetAll("da", ["Checkout", "navigation"], cancellationToken: CancellationToken.None);
        await Create(repository, cache: cache).GetAll("da", ["Checkout", "navigation"], cancellationToken: CancellationToken.None);

        Assert.Equal("{\"Checkout\":{\"title\":\"Betaling\"},\"navigation\":{\"title\":\"Menu\"}}", Assert.IsType<ContentResult>(result).Content);
        // Not the stored spelling, so served but not kept.
        Assert.Equal(2, repository.Reads);
    }

    [Fact]
    public async Task A_comma_separated_list_is_refused_with_how_to_ask_for_several()
    {
        var result = await Create(new TwoNamespacesRepository()).GetAll("da", ["checkout,navigation"], cancellationToken: CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("namespace=a&namespace=b", Assert.IsType<string>(badRequest.Value), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_namespace_with_a_comma_in_its_name_is_still_served()
    {
        var result = await Create(new TwoNamespacesRepository(moreNamespaces: "forms,legacy")).GetAll("da", ["forms,legacy"], cancellationToken: CancellationToken.None);

        Assert.Equal("{\"title\":\"forms,legacy\"}", Assert.IsType<ContentResult>(result).Content);
    }

    [Fact]
    public async Task A_repository_with_only_the_single_namespace_method_still_serves_several()
    {
        var result = await Create(new SingleNamespaceRepository()).GetAll("da", ["checkout", "navigation"], cancellationToken: CancellationToken.None);

        Assert.Equal("{\"checkout\":{\"title\":\"checkout\"},\"navigation\":{\"title\":\"navigation\"}}", Assert.IsType<ContentResult>(result).Content);
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            Create(new SingleNamespaceRepository()).GetAll("da", null, cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task Too_many_namespaces_at_once_are_refused()
    {
        var namespaces = Enumerable.Range(0, TranslationOutputController.MaximumNamespacesPerRequest + 1).Select(index => $"ns{index}").ToArray();

        var result = await Create(new UnusedRepository()).GetAll("da", namespaces, cancellationToken: CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Output_for_a_namespace_leaves_the_namespace_out()
    {
        var result = await Create(new CountingRepository()).GetAll("en", ["website"], cancellationToken: CancellationToken.None);

        Assert.Equal("{\"greeting\":\"Hello {name}\"}", Assert.IsType<ContentResult>(result).Content);
    }

    [Fact]
    public async Task Output_without_a_namespace_names_the_namespace_that_does_not_suit_the_format()
    {
        var result = await Create(new IcuRepository()).GetAll("en", null, "i18next-v4", CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.StartsWith("Namespace 'website' contains Icu messages", Assert.IsType<string>(conflict.Value), StringComparison.Ordinal);
    }

    [Fact]
    public async Task One_namespace_and_every_namespace_are_kept_apart()
    {
        var repository = new CountingRepository();
        var cache = new DeliveryResponseCache(TimeProvider.System);

        await Create(repository, cache: cache).GetAll("en", ["website"], cancellationToken: CancellationToken.None);
        await Create(repository, cache: cache).GetAll("en", null, cancellationToken: CancellationToken.None);
        await Create(repository, cache: cache).GetAll("en", null, cancellationToken: CancellationToken.None);

        Assert.Equal(2, repository.Reads);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task All_output_requires_a_locale(string? locale)
    {
        var controller = Create(new UnusedRepository());

        var result = await controller.GetAll(locale, ["website"], cancellationToken: CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("The locale query parameter is required.", badRequest.Value);
    }

    [Fact]
    public async Task All_output_accepts_i18next_v4_json_format()
    {
        var controller = Create(new EmptyRepository());

        var result = await controller.GetAll("en", ["website"], "i18next-v4", CancellationToken.None);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal("{}", content.Content);
        Assert.Equal("application/json; charset=utf-8", content.ContentType);
    }

    [Fact]
    public async Task I18next_output_rejects_an_icu_namespace()
    {
        var controller = Create(new IcuRepository());

        var result = await controller.GetAll("en", ["website"], "i18next-v4", CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("Icu messages", Assert.IsType<string>(conflict.Value), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Override_output_validates_the_full_namespace_syntax()
    {
        var controller = Create(new MixedRepository());

        var result = await controller.GetOverrides("en", ["website"], "i18next-v4", CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Output_is_public_and_shared_caches_may_keep_it_when_no_key_is_configured()
    {
        var controller = Create(new EmptyRepository());

        var result = await controller.GetAll("en", ["website"], cancellationToken: CancellationToken.None);

        Assert.IsType<ContentResult>(result);
        Assert.Equal("public,max-age=60", controller.Response.Headers.CacheControl.ToString());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("wrong-key", null)]
    [InlineData(null, "Bearer wrong-key")]
    [InlineData(null, "Basic delivery-key")]
    public async Task Output_refuses_a_request_without_the_configured_key(string? apiKey, string? authorization)
    {
        var controller = Create(new EmptyRepository(), configuredKey: "delivery-key", apiKey, authorization);

        var result = await controller.GetOverrides("en", ["website"], cancellationToken: CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal("Bearer", controller.Response.Headers.WWWAuthenticate.ToString());
    }

    [Theory]
    [InlineData("delivery-key", null)]
    [InlineData(null, "Bearer delivery-key")]
    public async Task Output_answers_the_configured_key_and_keeps_it_out_of_shared_caches(string? apiKey, string? authorization)
    {
        var controller = Create(new EmptyRepository(), configuredKey: "delivery-key", apiKey, authorization);

        var result = await controller.GetAll("en", ["website"], cancellationToken: CancellationToken.None);

        Assert.IsType<ContentResult>(result);
        Assert.Equal("private,max-age=60", controller.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task A_repeat_request_is_served_without_reading_the_database_again()
    {
        var repository = new CountingRepository();
        var cache = new DeliveryResponseCache(TimeProvider.System);

        var first = Assert.IsType<ContentResult>(await Create(repository, cache: cache).GetAll("en", ["website"], cancellationToken: CancellationToken.None));
        var second = Assert.IsType<ContentResult>(await Create(repository, cache: cache).GetAll("en", ["website"], cancellationToken: CancellationToken.None));

        Assert.Equal(1, repository.Reads);
        Assert.Equal(first.Content, second.Content);
    }

    [Fact]
    public async Task A_cleared_cache_reads_the_database_again()
    {
        var repository = new CountingRepository();
        var cache = new DeliveryResponseCache(TimeProvider.System);

        await Create(repository, cache: cache).GetAll("en", ["website"], cancellationToken: CancellationToken.None);
        cache.Clear();
        await Create(repository, cache: cache).GetAll("en", ["website"], cancellationToken: CancellationToken.None);

        Assert.Equal(2, repository.Reads);
    }

    [Fact]
    public async Task A_response_with_no_messages_is_not_kept()
    {
        var repository = new CountingRepository(empty: true);
        var cache = new DeliveryResponseCache(TimeProvider.System);

        await Create(repository, cache: cache).GetAll("xx", ["made-up"], cancellationToken: CancellationToken.None);
        await Create(repository, cache: cache).GetAll("xx", ["made-up"], cancellationToken: CancellationToken.None);

        Assert.Equal(2, repository.Reads);
    }

    [Fact]
    public async Task Overrides_and_all_are_kept_apart()
    {
        var repository = new CountingRepository();
        var cache = new DeliveryResponseCache(TimeProvider.System);

        await Create(repository, cache: cache).GetAll("en", ["website"], cancellationToken: CancellationToken.None);
        await Create(repository, cache: cache).GetOverrides("en", ["website"], cancellationToken: CancellationToken.None);

        Assert.Equal(2, repository.Reads);
    }

    [Fact]
    public async Task A_revalidation_answers_304_with_the_same_validator_and_caching_rules()
    {
        var repository = new CountingRepository();
        var cache = new DeliveryResponseCache(TimeProvider.System);
        var first = Create(repository, cache: cache);
        await first.GetAll("en", ["website"], cancellationToken: CancellationToken.None);
        var etag = first.Response.Headers.ETag.ToString();

        var second = Create(repository, cache: cache, ifNoneMatch: etag);
        var result = await second.GetAll("en", ["website"], cancellationToken: CancellationToken.None);

        Assert.Equal(StatusCodes.Status304NotModified, Assert.IsType<StatusCodeResult>(result).StatusCode);
        Assert.Equal(etag, second.Response.Headers.ETag.ToString());
        Assert.Equal("public,max-age=60", second.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task Another_spelling_of_a_stored_locale_is_served_but_not_kept()
    {
        // On a case-insensitive database, DA-DK finds the rows stored under da-DK.
        var repository = new CountingRepository(storedLocale: "da-DK");
        var cache = new DeliveryResponseCache(TimeProvider.System);

        var result = await Create(repository, cache: cache).GetAll("DA-DK", ["website"], cancellationToken: CancellationToken.None);
        await Create(repository, cache: cache).GetAll("DA-DK", ["website"], cancellationToken: CancellationToken.None);

        Assert.Contains("greeting", Assert.IsType<ContentResult>(result).Content, StringComparison.Ordinal);
        Assert.Equal(2, repository.Reads);
    }

    [Theory]
    [InlineData("W/{0}")]
    [InlineData("\"something-else\", {0}")]
    [InlineData("*")]
    public async Task A_weak_listed_or_wildcard_validator_still_gets_304(string ifNoneMatch)
    {
        var repository = new CountingRepository();
        var cache = new DeliveryResponseCache(TimeProvider.System);
        var first = Create(repository, cache: cache);
        await first.GetAll("en", ["website"], cancellationToken: CancellationToken.None);
        var etag = first.Response.Headers.ETag.ToString();

        var result = await Create(repository, cache: cache, ifNoneMatch: string.Format(System.Globalization.CultureInfo.InvariantCulture, ifNoneMatch, etag))
            .GetAll("en", ["website"], cancellationToken: CancellationToken.None);

        Assert.Equal(StatusCodes.Status304NotModified, Assert.IsType<StatusCodeResult>(result).StatusCode);
    }

    [Fact]
    public async Task A_different_validator_gets_the_response()
    {
        var result = await Create(new CountingRepository(), ifNoneMatch: "\"something-else\"")
            .GetAll("en", ["website"], cancellationToken: CancellationToken.None);

        Assert.IsType<ContentResult>(result);
    }

    [Theory]
    [InlineData(60, "public,max-age=60")]
    [InlineData(300, "public,max-age=300")]
    [InlineData(0, "public,no-cache")]
    public async Task The_lifetime_comes_from_the_setting(int maxAgeSeconds, string expected)
    {
        var controller = Create(new CountingRepository(), maxAgeSeconds: maxAgeSeconds);

        await controller.GetAll("en", ["website"], cancellationToken: CancellationToken.None);

        Assert.Equal(expected, controller.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task A_keyed_response_with_no_lifetime_stays_private()
    {
        var controller = Create(new CountingRepository(), configuredKey: "delivery-key", apiKey: "delivery-key", maxAgeSeconds: 0);

        await controller.GetAll("en", ["website"], cancellationToken: CancellationToken.None);

        Assert.Equal("private,no-cache", controller.Response.Headers.CacheControl.ToString());
    }

    private static TranslationOutputController Create(
        ITranslationMessageRepository repository,
        string? configuredKey = null,
        string? apiKey = null,
        string? authorization = null,
        DeliveryResponseCache? cache = null,
        int maxAgeSeconds = 60,
        string? ifNoneMatch = null)
    {
        var context = new DefaultHttpContext();
        if (apiKey is not null)
            context.Request.Headers[DeliveryApiKeyAuthenticator.HeaderName] = apiKey;
        if (authorization is not null)
            context.Request.Headers.Authorization = authorization;
        if (ifNoneMatch is not null)
            context.Request.Headers.IfNoneMatch = ifNoneMatch;
        var options = Options.Create(new DeliveryOptions { ApiKey = configuredKey, MaxAgeSeconds = maxAgeSeconds });
        return new TranslationOutputController(
            repository,
            new NestedJsonOutputSerializer(),
            new DeliveryApiKeyAuthenticator(options),
            cache ?? new DeliveryResponseCache(TimeProvider.System),
            options)
        {
            ControllerContext = new ControllerContext { HttpContext = context },
        };
    }

    /// <summary>One ICU message, or none, counting how often it is read.</summary>
    private sealed class CountingRepository(bool empty = false, string? storedLocale = null) : UnusedRepository
    {
        public int Reads { get; private set; }

        public override Task<IReadOnlyList<TranslationMessageView>> GetOutputMessagesAsync(string locale, IReadOnlyCollection<string> namespaces, CancellationToken cancellationToken)
        {
            Reads++;
            if (empty)
                return Task.FromResult<IReadOnlyList<TranslationMessageView>>([]);
            var now = DateTimeOffset.UtcNow;
            var message = new TranslationMessage(Guid.NewGuid(), new(Guid.NewGuid(), namespaces.FirstOrDefault() ?? "website", "greeting", storedLocale ?? locale),
                "Hello {name}", MessageFormat.Icu, new Dictionary<string, string> { ["name"] = "string" }, null,
                "revision", "checksum", null, now, now, TranslationMessageState.Active);
            return Task.FromResult<IReadOnlyList<TranslationMessageView>>([new(message, null)]);
        }
    }

    private class UnusedRepository : ITranslationMessageRepository
    {
        public Task<Page<TranslationMessageView>> QueryMessagesAsync(MessageQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TranslationFacetData> GetFacetDataAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TranslationMessageView?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TranslationOverride> SaveOverrideAsync(Guid messageId, string value, long? expectedVersion, string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteOverrideAsync(Guid messageId, long? expectedVersion, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TranslationMessageView>> GetOutputMessagesAsync(string locale, string messageNamespace, CancellationToken cancellationToken) => GetOutputMessagesAsync(locale, [messageNamespace], cancellationToken);
        public virtual Task<IReadOnlyList<TranslationMessageView>> GetOutputMessagesAsync(string locale, IReadOnlyCollection<string> namespaces, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class EmptyRepository : UnusedRepository
    {
        public override Task<IReadOnlyList<TranslationMessageView>> GetOutputMessagesAsync(string locale, IReadOnlyCollection<string> namespaces, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TranslationMessageView>>([]);
    }

    private sealed class IcuRepository : UnusedRepository
    {
        public override Task<IReadOnlyList<TranslationMessageView>> GetOutputMessagesAsync(string locale, IReadOnlyCollection<string> namespaces, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var message = new TranslationMessage(Guid.NewGuid(), new(Guid.NewGuid(), namespaces.FirstOrDefault() ?? "website", "greeting", locale),
                "Hello {name}", MessageFormat.Icu, new Dictionary<string, string> { ["name"] = "string" }, null,
                "revision", "checksum", null, now, now, TranslationMessageState.Active);
            return Task.FromResult<IReadOnlyList<TranslationMessageView>>([new(message, null)]);
        }
    }

    /// <summary>Namespaces that all have a <c>title</c>, which only stay apart under their own names, counting reads.</summary>
    private sealed class TwoNamespacesRepository(bool caseInsensitive = false, params string[] moreNamespaces) : UnusedRepository
    {
        public IReadOnlyCollection<string>? AskedFor { get; private set; }
        public int Reads { get; private set; }

        public override Task<IReadOnlyList<TranslationMessageView>> GetOutputMessagesAsync(string locale, IReadOnlyCollection<string> namespaces, CancellationToken cancellationToken)
        {
            AskedFor = namespaces;
            Reads++;
            var now = DateTimeOffset.UtcNow;
            TranslationMessageView View(string messageNamespace, string value) =>
                new(new TranslationMessage(Guid.NewGuid(), new(Guid.NewGuid(), messageNamespace, "title", locale),
                    value, MessageFormat.Icu, new Dictionary<string, string>(), null, "revision", "checksum", null,
                    now, now, TranslationMessageState.Active), null);
            IReadOnlyList<TranslationMessageView> all = [View("navigation", "Menu"), View("checkout", "Betaling"), View("search", "Søg"), .. moreNamespaces.Select(name => View(name, name))];
            // Like a database: on a case-insensitive one, Checkout finds the rows stored as checkout.
            var comparer = caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            return Task.FromResult<IReadOnlyList<TranslationMessageView>>(
                namespaces.Count == 0 ? all : all.Where(view => namespaces.Contains(view.Message.Identity.Namespace, comparer)).ToArray());
        }
    }

    /// <summary>A repository written before the namespace list existed: it implements only the single-namespace method.</summary>
    private sealed class SingleNamespaceRepository : ITranslationMessageRepository
    {
        public Task<Page<TranslationMessageView>> QueryMessagesAsync(MessageQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TranslationFacetData> GetFacetDataAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TranslationMessageView?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TranslationOverride> SaveOverrideAsync(Guid messageId, string value, long? expectedVersion, string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteOverrideAsync(Guid messageId, long? expectedVersion, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<TranslationMessageView>> GetOutputMessagesAsync(string locale, string messageNamespace, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var message = new TranslationMessage(Guid.NewGuid(), new(Guid.NewGuid(), messageNamespace, "title", locale),
                messageNamespace, MessageFormat.Icu, new Dictionary<string, string>(), null, "revision", "checksum", null,
                now, now, TranslationMessageState.Active);
            return Task.FromResult<IReadOnlyList<TranslationMessageView>>([new(message, null)]);
        }
    }

    private sealed class MixedRepository : UnusedRepository
    {
        public override Task<IReadOnlyList<TranslationMessageView>> GetOutputMessagesAsync(string locale, IReadOnlyCollection<string> namespaces, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            TranslationMessageView View(string key, string value, MessageFormat format, bool hasOverride)
            {
                var message = new TranslationMessage(Guid.NewGuid(), new(Guid.NewGuid(), namespaces.FirstOrDefault() ?? "website", key, locale),
                    value, format, new Dictionary<string, string>(), null, "revision", "checksum", null,
                    now, now, TranslationMessageState.Active);
                var translationOverride = hasOverride
                    ? new TranslationOverride(message.Id, value, "checksum", 1, now, "user", now, "user")
                    : null;
                return new(message, translationOverride);
            }

            IReadOnlyList<TranslationMessageView> all =
            [
                View("icu", "Hello {name}", MessageFormat.Icu, false),
                View("i18next", "Hello {{name}}", MessageFormat.I18NextV4, true),
            ];
            return Task.FromResult(all);
        }
    }
}
