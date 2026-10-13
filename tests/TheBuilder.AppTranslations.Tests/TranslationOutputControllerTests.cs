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
    public async Task All_output_requires_a_namespace(string? messageNamespace)
    {
        var controller = Create(new UnusedRepository());

        var result = await controller.GetAll("da", messageNamespace, cancellationToken: CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("The namespace query parameter is required.", badRequest.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task All_output_requires_a_locale(string? locale)
    {
        var controller = Create(new UnusedRepository());

        var result = await controller.GetAll(locale, "website", cancellationToken: CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("The locale query parameter is required.", badRequest.Value);
    }

    [Fact]
    public async Task All_output_accepts_i18next_v4_json_format()
    {
        var controller = Create(new EmptyRepository());

        var result = await controller.GetAll("en", "website", "i18next-v4", CancellationToken.None);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal("{}", content.Content);
        Assert.Equal("application/json; charset=utf-8", content.ContentType);
    }

    [Fact]
    public async Task I18next_output_rejects_an_icu_namespace()
    {
        var controller = Create(new IcuRepository());

        var result = await controller.GetAll("en", "website", "i18next-v4", CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("Icu messages", Assert.IsType<string>(conflict.Value), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Override_output_validates_the_full_namespace_syntax()
    {
        var controller = Create(new MixedRepository());

        var result = await controller.GetOverrides("en", "website", "i18next-v4", CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Output_is_public_and_shared_caches_may_keep_it_when_no_key_is_configured()
    {
        var controller = Create(new EmptyRepository());

        var result = await controller.GetAll("en", "website", cancellationToken: CancellationToken.None);

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

        var result = await controller.GetOverrides("en", "website", cancellationToken: CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal("Bearer", controller.Response.Headers.WWWAuthenticate.ToString());
    }

    [Theory]
    [InlineData("delivery-key", null)]
    [InlineData(null, "Bearer delivery-key")]
    public async Task Output_answers_the_configured_key_and_keeps_it_out_of_shared_caches(string? apiKey, string? authorization)
    {
        var controller = Create(new EmptyRepository(), configuredKey: "delivery-key", apiKey, authorization);

        var result = await controller.GetAll("en", "website", cancellationToken: CancellationToken.None);

        Assert.IsType<ContentResult>(result);
        Assert.Equal("private,max-age=60", controller.Response.Headers.CacheControl.ToString());
    }

    private static TranslationOutputController Create(
        ITranslationMessageRepository repository,
        string? configuredKey = null,
        string? apiKey = null,
        string? authorization = null)
    {
        var context = new DefaultHttpContext();
        if (apiKey is not null)
            context.Request.Headers[DeliveryApiKeyAuthenticator.HeaderName] = apiKey;
        if (authorization is not null)
            context.Request.Headers.Authorization = authorization;
        var authenticator = new DeliveryApiKeyAuthenticator(Options.Create(new DeliveryOptions { ApiKey = configuredKey }));
        return new TranslationOutputController(repository, new NestedJsonOutputSerializer(), authenticator)
        {
            ControllerContext = new ControllerContext { HttpContext = context },
        };
    }

    private class UnusedRepository : ITranslationMessageRepository
    {
        public Task<Page<TranslationMessageView>> QueryMessagesAsync(MessageQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TranslationFacetData> GetFacetDataAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TranslationMessageView?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TranslationOverride> SaveOverrideAsync(Guid messageId, string value, long? expectedVersion, string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteOverrideAsync(Guid messageId, long? expectedVersion, CancellationToken cancellationToken) => throw new NotSupportedException();
        public virtual Task<IReadOnlyList<TranslationMessageView>> GetOutputMessagesAsync(string locale, string messageNamespace, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class EmptyRepository : UnusedRepository
    {
        public override Task<IReadOnlyList<TranslationMessageView>> GetOutputMessagesAsync(string locale, string messageNamespace, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TranslationMessageView>>([]);
    }

    private sealed class IcuRepository : UnusedRepository
    {
        public override Task<IReadOnlyList<TranslationMessageView>> GetOutputMessagesAsync(string locale, string messageNamespace, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var message = new TranslationMessage(Guid.NewGuid(), new(Guid.NewGuid(), messageNamespace, "greeting", locale),
                "Hello {name}", MessageFormat.Icu, new Dictionary<string, string> { ["name"] = "string" }, null,
                "revision", "checksum", null, now, now, TranslationMessageState.Active);
            return Task.FromResult<IReadOnlyList<TranslationMessageView>>([new(message, null)]);
        }
    }

    private sealed class MixedRepository : UnusedRepository
    {
        public override Task<IReadOnlyList<TranslationMessageView>> GetOutputMessagesAsync(string locale, string messageNamespace, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            TranslationMessageView View(string key, string value, MessageFormat format, bool hasOverride)
            {
                var message = new TranslationMessage(Guid.NewGuid(), new(Guid.NewGuid(), messageNamespace, key, locale),
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
