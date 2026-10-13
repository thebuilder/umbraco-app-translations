using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace TheBuilder.AppTranslations.Authorization;

internal sealed class AppTranslationsSectionAccessRequirement : IAuthorizationRequirement;

internal sealed class AppTranslationsSectionAccessHandler(IBackOfficeUserSections sections)
    : AuthorizationHandler<AppTranslationsSectionAccessRequirement>
{
    internal const string SectionAlias = Constants.SectionAlias;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AppTranslationsSectionAccessRequirement requirement)
    {
        if (await sections.GetAllowedSectionsAsync(context.User) is { } allowed && allowed.Contains(SectionAlias, StringComparer.Ordinal))
            context.Succeed(requirement);
    }
}

/// <summary>The sections the signed-in backoffice user can open, or null when there is no such user.</summary>
internal interface IBackOfficeUserSections
{
    Task<IEnumerable<string>?> GetAllowedSectionsAsync(ClaimsPrincipal principal);
}

/// <summary>
/// The current backoffice user when Umbraco has one for the request, and otherwise the user a
/// management API bearer token names. Umbraco 17 also put the sections in the token's claims, but
/// Umbraco 18 dropped that claim, so the user is the one source that works across both.
/// </summary>
internal sealed class UmbracoBackOfficeUserSections(IBackOfficeSecurityAccessor securityAccessor, IUserService users)
    : IBackOfficeUserSections
{
    public async Task<IEnumerable<string>?> GetAllowedSectionsAsync(ClaimsPrincipal principal)
    {
        if (securityAccessor.BackOfficeSecurity?.CurrentUser is { } current)
            return current.AllowedSections;
        foreach (var identity in principal.Identities)
        {
            if (identity.GetUserKey() is { } key && await users.GetAsync(key) is { } user)
                return user.AllowedSections;
        }
        return null;
    }
}
