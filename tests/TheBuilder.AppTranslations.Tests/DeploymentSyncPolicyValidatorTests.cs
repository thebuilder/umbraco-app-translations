using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TheBuilder.AppTranslations.Authorization;

namespace TheBuilder.AppTranslations.Tests;

/// <summary>
/// A deployment sync policy that cannot work stops the site at startup, rather than leaving the
/// first deployment to find out.
/// </summary>
public sealed class DeploymentSyncPolicyValidatorTests
{
    private const string TokenScheme = "TestToken";

    [Fact]
    public void Accepts_a_site_that_registers_no_policy()
    {
        Assert.True(Validate(policy: null).Succeeded);
    }

    [Fact]
    public void Accepts_a_policy_whose_schemes_are_registered()
    {
        Assert.True(Validate(rule => rule.AddAuthenticationSchemes(TokenScheme).RequireAuthenticatedUser()).Succeeded);
    }

    [Fact]
    public void Refuses_a_policy_that_names_no_authentication_scheme()
    {
        // It would be checked against the default scheme's user, often signed in by a cookie.
        var result = Validate(rule => rule.RequireAuthenticatedUser());

        Assert.True(result.Failed);
        Assert.Contains("must name the authentication schemes", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Refuses_a_policy_whose_scheme_nothing_registers()
    {
        // Such as API OIDC's scheme, named in Program.cs on a site without the package.
        var result = Validate(rule => rule.AddAuthenticationSchemes("TheBuilder.Oidc").RequireAuthenticatedUser());

        Assert.True(result.Failed);
        Assert.Contains("'TheBuilder.Oidc'", result.FailureMessage, StringComparison.Ordinal);
    }

    private static ValidateOptionsResult Validate(Action<AuthorizationPolicyBuilder>? policy)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, NoTokenHandler>(TokenScheme, null);
        using var provider = services.BuildServiceProvider();

        var options = new AuthorizationOptions();
        if (policy is not null)
            options.AddPolicy(TranslationPolicies.DeploymentSync, policy);
        return new DeploymentSyncPolicyValidator(provider.GetRequiredService<IAuthenticationSchemeProvider>())
            .Validate(Options.DefaultName, options);
    }

    private sealed class NoTokenHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory loggerFactory,
        System.Text.Encodings.Web.UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
    }
}
