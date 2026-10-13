using Microsoft.AspNetCore.Authorization;
using Umbraco.Cms.Core.Security;
using Umbraco.Extensions;

namespace TheBuilder.AppTranslations.Authorization;

internal sealed class AppTranslationsSectionAccessRequirement : IAuthorizationRequirement;

internal sealed class AppTranslationsSectionAccessHandler(IBackOfficeSecurityAccessor securityAccessor)
    : AuthorizationHandler<AppTranslationsSectionAccessRequirement>
{
    internal const string SectionAlias = Constants.SectionAlias;

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AppTranslationsSectionAccessRequirement requirement)
    {
#pragma warning disable CS0618 // Management API bearer requests expose allowed sections through this Umbraco claims API, not IUser.
        if (context.User.Identities.Any(identity => identity.GetAllowedApplications().Contains(SectionAlias, StringComparer.Ordinal)))
#pragma warning restore CS0618
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var security = securityAccessor.BackOfficeSecurity;
        var currentUser = security?.CurrentUser;
        if (currentUser is not null && security!.UserHasSectionAccess(SectionAlias, currentUser))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
