using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuilder.AppTranslations.Authorization;
using TheBuilder.AppTranslations.AutomationApi;
using TheBuilder.AppTranslations.Configuration;
using TheBuilder.AppTranslations.Core.Messages;
using TheBuilder.AppTranslations.Core.Persistence;
using TheBuilder.AppTranslations.Core.Sources;
using TheBuilder.AppTranslations.Core.Synchronization;
using TheBuilder.AppTranslations.Core.Validation;

namespace TheBuilder.AppTranslations.Tests;

public sealed class DeploymentSyncControllerTests
{
    private const string ApiKey = "test-deployment-sync-key-32-characters";
    private const string TokenHeader = "X-Test-Token";
    private const string TokenScheme = "TestToken";
    private const string DeployPolicy = "Deploy";

    [Fact]
    public async Task Rejects_requests_when_deployment_sync_is_not_configured()
    {
        var controller = CreateController(apiKey: null);

        var response = await controller.SyncSource("sample", CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, problem.StatusCode);
    }

    [Fact]
    public async Task Rejects_an_invalid_bearer_token()
    {
        var controller = CreateController();
        controller.Request.Headers.Authorization = "Bearer incorrect";

        var response = await controller.SyncSource("sample", CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(response.Result);
        Assert.Equal("Bearer", controller.Response.Headers.WWWAuthenticate);
    }

    [Fact]
    public async Task Accepts_a_request_that_satisfies_the_policy_without_a_key()
    {
        var source = Source(enabled: true);
        var controller = CreateController(apiKey: null, policy: DeployPolicy, source: source);
        controller.Request.Headers[TokenHeader] = "GitHubActions";

        var response = await controller.SyncSource(source.Alias, CancellationToken.None);

        Assert.IsType<OkObjectResult>(response.Result);
    }

    [Fact]
    public async Task Asks_for_a_token_rather_than_a_key_when_only_the_policy_is_accepted()
    {
        var controller = CreateController(apiKey: null, policy: DeployPolicy);

        var response = await controller.SyncSource("sample", CancellationToken.None);

        // Not 503: the endpoint is on, the request just did not bring a token.
        Assert.IsType<UnauthorizedResult>(response.Result);
        Assert.False(controller.Response.Headers.ContainsKey("WWW-Authenticate"));
    }

    [Fact]
    public async Task Rejects_a_token_the_scheme_refused()
    {
        var controller = CreateController(apiKey: null, policy: DeployPolicy);
        controller.Request.Headers[TokenHeader] = FakeTokenHandler.RefusedToken;

        var response = await controller.SyncSource("sample", CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(response.Result);
    }

    [Fact]
    public async Task Forbids_a_valid_token_the_policy_does_not_accept()
    {
        // Such as a frontend's token, trusted to read the Delivery API but not to start a sync.
        var controller = CreateController(apiKey: null, policy: DeployPolicy);
        controller.Request.Headers[TokenHeader] = "Vercel";

        var response = await controller.SyncSource("sample", CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, problem.StatusCode);
    }

    [Fact]
    public async Task Ignores_tokens_unless_a_policy_is_configured()
    {
        var controller = CreateController();
        controller.Request.Headers[TokenHeader] = "GitHubActions";

        var response = await controller.SyncSource("sample", CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(response.Result);
    }

    [Fact]
    public async Task Still_accepts_the_key_when_a_policy_is_configured_too()
    {
        var source = Source(enabled: true);
        var controller = CreateController(policy: DeployPolicy, source: source);
        controller.Request.Headers.Authorization = $"Bearer {ApiKey}";

        var response = await controller.SyncSource(source.Alias, CancellationToken.None);

        Assert.IsType<OkObjectResult>(response.Result);
    }

    [Fact]
    public async Task Synchronizes_an_enabled_source_selected_by_alias()
    {
        var source = Source(enabled: true);
        var store = new FakeStore(source);
        var controller = CreateController(source: source, store: store);
        controller.Request.Headers.Authorization = $"Bearer {ApiKey}";

        var response = await controller.SyncSource(source.Alias, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var result = Assert.IsType<TranslationSyncResult>(ok.Value);
        Assert.Equal(TranslationSyncStatus.Succeeded, result.Status);
        Assert.Single(store.SynchronizedMessages);
    }

    [Fact]
    public async Task Does_not_synchronize_a_disabled_source()
    {
        var source = Source(enabled: false);
        var controller = CreateController(source: source);
        controller.Request.Headers.Authorization = $"Bearer {ApiKey}";

        var response = await controller.SyncSource(source.Alias, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(response.Result);
    }

    [Fact]
    public async Task Rejects_an_alias_that_is_not_one_route_safe_segment()
    {
        var controller = CreateController();
        controller.Request.Headers.Authorization = $"Bearer {ApiKey}";

        var response = await controller.SyncSource("sample/messages", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Fact]
    public async Task Does_not_expose_internal_failure_details_to_automation_clients()
    {
        var source = Source(enabled: true);
        var store = new FakeStore(source) { ApplyFailure = new InvalidOperationException("Translations:PrivateApiKey was missing") };
        var controller = CreateController(source: source, store: store);
        controller.Request.Headers.Authorization = $"Bearer {ApiKey}";

        var response = await controller.SyncSource(source.Alias, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(response.Result);
        var details = Assert.IsType<ProblemDetails>(problem.Value);
        Assert.Equal(StatusCodes.Status502BadGateway, details.Status);
        Assert.DoesNotContain("PrivateApiKey", details.Detail, StringComparison.Ordinal);
    }

    private static DeploymentSyncController CreateController(
        string? apiKey = ApiKey,
        string? policy = null,
        TranslationSourceDefinition? source = null,
        FakeStore? store = null)
    {
        store ??= new FakeStore(source);
        var engine = new TranslationSyncEngine(
            new FakeTransport(),
            new NestedJsonParser(new MessageFormatValidator()),
            store,
            TimeProvider.System);
        var authenticator = new DeploymentSyncAuthenticator(
            Options.Create(new DeploymentSyncOptions { ApiKey = apiKey, AuthorizationPolicy = policy }));

        // A token scheme and the policies a site would register, run through ASP.NET's own
        // authentication and authorization services as on a site.
        // AddMvcCore for the ProblemDetailsFactory that Problem() reads from the request's services.
        var services = new ServiceCollection().AddLogging();
        services.AddMvcCore();
        services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, FakeTokenHandler>(TokenScheme, null);
        services.AddAuthorization(authorization =>
            authorization.AddPolicy(DeployPolicy, rule => rule.AddAuthenticationSchemes(TokenScheme).RequireClaim("issuer", "GitHubActions")));
        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        return new DeploymentSyncController(store, engine, authenticator, NullLogger<DeploymentSyncController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }

    private static TranslationSourceDefinition Source(bool enabled) => new(
        Guid.NewGuid(),
        "sample",
        "Sample",
        enabled,
        new HttpTranslationTransportOptions("https://translations.example/{locale}.json"),
        new TranslationParserOptions(["en"], "website", NamespaceMode.Fixed));

    /// <summary>
    /// Stands in for a token scheme such as API OIDC's: the header holds the issuer the token came
    /// from, and <see cref="RefusedToken"/> is one the scheme refuses.
    /// </summary>
    private sealed class FakeTokenHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
    {
        public const string RefusedToken = "refused";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string? issuer = Request.Headers[TokenHeader];
            if (string.IsNullOrEmpty(issuer))
                return Task.FromResult(AuthenticateResult.NoResult());
            if (issuer == RefusedToken)
                return Task.FromResult(AuthenticateResult.Fail("The token is not valid."));

            var identity = new ClaimsIdentity([new Claim("issuer", issuer)], Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }

    private sealed class FakeTransport : ITranslationSourceTransport
    {
        public string Kind => "http";

        public Task<TranslationSourcePayload> FetchAsync(
            TranslationSourceDefinition source,
            TranslationFetchContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult(new TranslationSourcePayload(
                new MemoryStream(Encoding.UTF8.GetBytes("{\"greeting\":\"Hello\"}")),
                context.Locale,
                "application/json",
                null,
                null,
                "revision-1"));
    }

    private sealed class FakeStore(TranslationSourceDefinition? source) : ITranslationSourceRepository, ITranslationSynchronizationStore
    {
        public IReadOnlyCollection<TranslationSourceMessage> SynchronizedMessages { get; private set; } = [];
        public Exception? ApplyFailure { get; init; }

        public Task<IReadOnlyList<TranslationSourceDefinition>> GetSourcesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TranslationSourceDefinition>>(source is null ? [] : [source]);

        public Task<TranslationSourceDefinition?> GetSourceAsync(Guid sourceId, CancellationToken cancellationToken) =>
            Task.FromResult(source?.Id == sourceId ? source : null);

        public Task<TranslationSourceDefinition?> GetSourceByAliasAsync(string alias, CancellationToken cancellationToken) =>
            Task.FromResult(source?.Alias == alias ? source : null);

        public Task SaveSourceAsync(TranslationSourceDefinition definition, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteSourceAsync(Guid sourceId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<TranslationSyncResult>> GetSyncHistoryAsync(Guid sourceId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TranslationSyncResult>>([]);

        public Task<IReadOnlyList<TranslationSourceStatus>> GetSourceStatusesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TranslationSourceStatus>>([]);

        public Task<TranslationSyncResult> ApplySynchronizationAsync(
            TranslationSourceDefinition definition,
            TranslationSynchronizationLease lease,
            IReadOnlyCollection<TranslationSourceMessage> messages,
            string revision,
            DateTimeOffset startedAt,
            CancellationToken cancellationToken)
        {
            if (ApplyFailure is not null) throw ApplyFailure;
            SynchronizedMessages = messages;
            return Task.FromResult(new TranslationSyncResult(
                Guid.NewGuid(), definition.Id, revision, startedAt, TimeProvider.System.GetUtcNow(),
                TranslationSyncStatus.Succeeded, messages.Count, 0, 0, 0));
        }

        public Task RecordFailedSynchronizationAsync(TranslationSyncResult result, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<TranslationSynchronizationLease?> TryAcquireSynchronizationLeaseAsync(TranslationSourceDefinition definition, TimeSpan duration, CancellationToken cancellationToken) =>
            Task.FromResult<TranslationSynchronizationLease?>(new(definition.Id, Guid.NewGuid()));

        public Task<TranslationSourceDefinition?> GetSynchronizationSourceAsync(TranslationSynchronizationLease lease, CancellationToken cancellationToken) =>
            Task.FromResult(source?.Id == lease.SourceId ? source : null);

        public Task ReleaseSynchronizationLeaseAsync(TranslationSynchronizationLease lease, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> RenewSynchronizationLeaseAsync(TranslationSynchronizationLease lease, TimeSpan duration, CancellationToken cancellationToken) => Task.FromResult(true);
    }
}
