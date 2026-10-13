using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TheBuilder.AppTranslations.Authorization;
using Umbraco.Cms.Core.Security;
using Umbraco.Extensions;

namespace TheBuilder.AppTranslations.Tests;

public sealed class TranslationAuthorizationTests
{
    [Fact]
    public async Task Section_access_is_granted_by_the_package_section()
    {
        var context = await SectionAccess([AppTranslationsSectionAccessHandler.SectionAlias]);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Administrator_access_uses_the_authenticated_management_api_roles()
    {
        var identity = BackofficeIdentity(
            allowedSections: [AppTranslationsSectionAccessHandler.SectionAlias],
            roles: [Umbraco.Cms.Core.Constants.Security.AdminGroupAlias]);
        var requirement = new BackofficeAdministratorRequirement();
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(identity), null);
        var handler = new BackofficeAdministratorHandler(new EmptyBackOfficeSecurityAccessor());

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Section_access_denies_a_token_without_the_package_section()
    {
        var context = await SectionAccess([]);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Section_access_is_not_granted_by_Umbracos_translation_section()
    {
        var context = await SectionAccess([Umbraco.Cms.Core.Constants.Applications.Translation]);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Administrator_access_denies_a_non_administrator_token()
    {
        var requirement = new BackofficeAdministratorRequirement();
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(BackofficeIdentity([AppTranslationsSectionAccessHandler.SectionAlias], [])), null);

        await new BackofficeAdministratorHandler(new EmptyBackOfficeSecurityAccessor()).HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Section_access_is_denied_without_a_backoffice_user()
    {
        var context = await SectionAccess(null);

        Assert.False(context.HasSucceeded);
    }

    private static async Task<AuthorizationHandlerContext> SectionAccess(IEnumerable<string>? allowedSections)
    {
        var requirement = new AppTranslationsSectionAccessRequirement();
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(new ClaimsIdentity("BackOffice")), null);
        await new AppTranslationsSectionAccessHandler(new FixedSections(allowedSections)).HandleAsync(context);
        return context;
    }

    private sealed class FixedSections(IEnumerable<string>? allowed) : IBackOfficeUserSections
    {
        public Task<IEnumerable<string>?> GetAllowedSectionsAsync(ClaimsPrincipal principal) => Task.FromResult(allowed);
    }

    private static ClaimsIdentity BackofficeIdentity(IReadOnlyCollection<string> allowedSections, IReadOnlyCollection<string> roles)
    {
        var identity = new ClaimsIdentity("BackOffice");
        identity.AddRequiredClaims(
            "1",
            Guid.NewGuid(),
            "admin@example.com",
            "Administrator",
            [],
            [],
            "en-US",
            "security-stamp",
            allowedSections,
            roles);
        return identity;
    }

    private sealed class EmptyBackOfficeSecurityAccessor : IBackOfficeSecurityAccessor
    {
        public IBackOfficeSecurity? BackOfficeSecurity => null;

        public IDisposable Override(IBackOfficeSecurity backOfficeSecurity) =>
            throw new NotSupportedException();
    }
}
