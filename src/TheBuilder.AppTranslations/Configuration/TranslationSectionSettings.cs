using Umbraco.Cms.Core.Services;

namespace TheBuilder.AppTranslations.Configuration;

/// <summary>
/// Whether the backoffice hides Umbraco's own Translation section while the site has no Dictionary
/// items. That section holds only the Dictionary, so on a site without any it is a second
/// translation section with nothing in it, next to this package's.
/// </summary>
/// <remarks>
/// Hidden in the backoffice rather than taken away from user groups, so uninstalling the package
/// brings the section back without anyone having to remember what was changed. It stays visible
/// whenever Dictionary items exist, whatever this says, so no one's Dictionary is ever out of reach.
/// </remarks>
internal sealed record TranslationSectionSettings(bool HideWhileDictionaryIsEmpty)
{
    /// <summary>On: hiding an empty section costs nothing, and the setting can bring it back.</summary>
    public static TranslationSectionSettings Default { get; } = new(HideWhileDictionaryIsEmpty: true);

    /// <summary>Hidden only when the setting is on and there is no Dictionary to reach.</summary>
    public bool HidesSection(bool hasDictionaryItems) => HideWhileDictionaryIsEmpty && !hasDictionaryItems;
}

/// <summary>Where <see cref="TranslationSectionSettings"/> is kept.</summary>
/// <remarks>
/// Public only because the public <see cref="ManagementApi.TranslationSectionController"/> takes it
/// in its constructor, and ASP.NET only calls public constructors. Its members are internal, so it is
/// not an extension point.
/// </remarks>
public interface ITranslationSectionSettingsStore
{
    internal TranslationSectionSettings Get();

    internal void Save(TranslationSectionSettings settings);
}

/// <summary>An unset or unreadable value means the default, which only ever hides an empty section.</summary>
internal sealed class TranslationSectionSettingsStore(IKeyValueService keyValues)
    : KeyValueSettingsStore<TranslationSectionSettings>(
        keyValues,
        Constants.PackageName + ".TranslationSection",
        TranslationSectionSettings.Default),
      ITranslationSectionSettingsStore;
