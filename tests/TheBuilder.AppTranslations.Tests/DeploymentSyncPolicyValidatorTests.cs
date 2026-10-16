using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TheBuilder.AppTranslations.Authorization;
using TheBuilder.AppTranslations.Configuration;

namespace TheBuilder.AppTranslations.Tests;

/// <summary>
/// A policy that cannot work stops the site at startup, rather than leaving the first deployment to
/// find out with a 503.
/// </summary>
public sealed class DeploymentSyncPolicyValidatorTests
{
    private const string TokenScheme = "TestToken";

    [Fact]
    public void Accepts_no_policy()
    {
        Assert.True(Validate(policy: null).Succeeded);
    }

    [Fact]
    public void Accepts_a_policy_whose_schemes_are_registered()
    {
        Assert.True(Validate("Deploy").Succeeded);
    }

    [Fact]
    public void Refuses_a_policy_that_is_not_registered()
    {
        var result = Validate("Misspelled");

        Assert.True(result.Failed);
        Assert.Contains("not registered", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Refuses_a_policy_that_names_no_authentication_scheme()
    {
        // It would be checked against the default scheme's user, often signed in by a cookie.
        var result = Validate("Schemeless");

        Assert.True(result.Failed);
        Assert.Contains("must name the authentication schemes", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Refuses_a_policy_whose_scheme_nothing_registers()
    {
        // Such as API OIDC's policy registered by hand on a site without the package.
        var result = Validate("MissingScheme");

        Assert.True(result.Failed);
        Assert.Contains("'TheBuilder.Oidc'", result.FailureMessage, StringComparison.Ordinal);
    }

    private static ValidateOptionsResult Validate(string? policy)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, NoTokenHandler>(TokenScheme, null);
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Deploy", rule => rule.AddAuthenticationSchemes(TokenScheme).RequireAuthenticatedUser());
            options.AddPolicy("Schemeless", rule => rule.RequireAuthenticatedUser());
            options.AddPolicy("MissingScheme", rule => rule.AddAuthenticationSchemes("TheBuilder.Oidc").RequireAuthenticatedUser());
        });
        using var provider = services.BuildServiceProvider();
        var validator = new DeploymentSyncPolicyValidator(
            provider.GetRequiredService<IAuthorizationPolicyProvider>(),
            provider.GetRequiredService<IAuthenticationSchemeProvider>());
        return validator.Validate(Options.DefaultName, new DeploymentSyncOptions { AuthorizationPolicy = policy });
    }

    private sealed class NoTokenHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory loggerFactory,
        System.Text.Encodings.Web.UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
    }
}
