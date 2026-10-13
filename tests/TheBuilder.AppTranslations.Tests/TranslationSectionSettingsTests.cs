using TheBuilder.AppTranslations.Configuration;

namespace TheBuilder.AppTranslations.Tests;

public sealed class TranslationSectionSettingsTests
{
    [Fact]
    public void A_site_that_never_chose_hides_the_empty_section()
    {
        var settings = new TranslationSectionSettingsStore(new FakeKeyValues()).Get();

        Assert.True(settings.HideWhileDictionaryIsEmpty);
        Assert.True(settings.HidesSection(hasDictionaryItems: false));
    }

    [Fact]
    public void Dictionary_items_keep_the_section_visible_whatever_the_setting()
    {
        Assert.False(new TranslationSectionSettings(HideWhileDictionaryIsEmpty: true).HidesSection(hasDictionaryItems: true));
        Assert.False(new TranslationSectionSettings(HideWhileDictionaryIsEmpty: false).HidesSection(hasDictionaryItems: true));
    }

    [Fact]
    public void Turning_the_setting_off_shows_the_empty_section()
    {
        var values = new FakeKeyValues();
        new TranslationSectionSettingsStore(values).Save(new(HideWhileDictionaryIsEmpty: false));

        var settings = new TranslationSectionSettingsStore(values).Get();

        Assert.False(settings.HidesSection(hasDictionaryItems: false));
    }

    [Fact]
    public void An_unreadable_value_falls_back_to_the_default()
    {
        var values = new FakeKeyValues();
        values.SetValue(Constants.PackageName + ".TranslationSection", "not json");

        Assert.Equal(TranslationSectionSettings.Default, new TranslationSectionSettingsStore(values).Get());
    }
}
