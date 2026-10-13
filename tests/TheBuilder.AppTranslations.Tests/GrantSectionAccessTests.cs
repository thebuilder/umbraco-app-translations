using TheBuilder.AppTranslations.Migrations;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace TheBuilder.AppTranslations.Tests;

public sealed class GrantSectionAccessTests
{
    [Fact]
    public void A_group_that_can_open_Translation_is_granted_the_section()
    {
        Assert.True(GrantSectionAccess.ShouldGrant([UmbracoConstants.Applications.Translation]));
        Assert.True(GrantSectionAccess.ShouldGrant([UmbracoConstants.Applications.Content, UmbracoConstants.Applications.Translation]));
    }

    [Fact]
    public void A_group_without_Translation_is_left_alone()
    {
        Assert.False(GrantSectionAccess.ShouldGrant([]));
        Assert.False(GrantSectionAccess.ShouldGrant([UmbracoConstants.Applications.Content, UmbracoConstants.Applications.Media]));
    }

    [Fact]
    public void A_group_that_already_has_the_section_is_not_granted_it_again()
    {
        Assert.False(GrantSectionAccess.ShouldGrant([UmbracoConstants.Applications.Translation, Constants.SectionAlias]));
    }
}
