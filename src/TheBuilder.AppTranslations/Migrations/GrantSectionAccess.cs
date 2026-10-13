using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace TheBuilder.AppTranslations.Migrations;

/// <summary>
/// Opens the package's section to every user group that can open Umbraco's Translation section,
/// which on a fresh site is Administrators and Translators.
/// </summary>
/// <remarks>
/// The section is the package's own, so no group has it until something grants it, and a section
/// nobody can open looks like a package that installed and did nothing. The Translation section is
/// the closest existing statement of who works on a site's text. Only adds: what else a group can
/// open is the site's business. Through the service rather than SQL, so the user group cache sees
/// the change.
/// </remarks>
internal sealed class GrantSectionAccess(IMigrationContext context, IUserGroupService userGroups)
    : AsyncMigrationBase(context)
{
    protected override async Task MigrateAsync()
    {
        var groups = await userGroups.GetAllAsync(0, int.MaxValue);
        foreach (var group in groups.Items)
        {
            if (!ShouldGrant(group.AllowedSections))
                continue;

            group.AddAllowedSection(Constants.SectionAlias);
            var result = await userGroups.UpdateAsync(group, UmbracoConstants.Security.SuperUserKey);
            if (!result.Success)
                Logger.LogWarning("Could not open {Section} to the {UserGroup} user group: {Status}", Constants.SectionAlias, group.Alias, result.Status);
        }
    }

    /// <summary>A group that can open Umbraco's Translation section and not yet this one.</summary>
    internal static bool ShouldGrant(IEnumerable<string> allowedSections)
    {
        var sections = allowedSections.ToHashSet(StringComparer.Ordinal);
        return sections.Contains(UmbracoConstants.Applications.Translation) && !sections.Contains(Constants.SectionAlias);
    }
}
