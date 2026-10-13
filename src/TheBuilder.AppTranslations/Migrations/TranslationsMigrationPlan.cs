using Umbraco.Cms.Core.Packaging;

namespace TheBuilder.AppTranslations.Migrations;

internal sealed class TranslationsMigrationPlan : PackageMigrationPlan
{
    public TranslationsMigrationPlan() : base(Constants.PackageName) { }

    protected override void DefinePlan()
    {
        To<InstallTranslationSchema>(new Guid("a4878e28-55f4-423a-bb0e-6bfb0baa0e93"));
        To<GrantSectionAccess>(new Guid("9c800eac-3a11-4f3a-b1a4-8211fe0c2420"));
    }
}
