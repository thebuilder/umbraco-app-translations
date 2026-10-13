using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheBuilder.AppTranslations.Authorization;
using TheBuilder.AppTranslations.Configuration;

namespace TheBuilder.AppTranslations.ManagementApi;

/// <summary>Whether Umbraco's Translation section is hidden, and why.</summary>
/// <param name="HideWhileDictionaryIsEmpty">The setting an administrator controls.</param>
/// <param name="HasDictionaryItems">Whether the site has any Dictionary items, which keeps the section visible.</param>
/// <param name="Hidden">What the backoffice does: hide the section only when both say so.</param>
public sealed record TranslationSectionResponse(bool HideWhileDictionaryIsEmpty, bool HasDictionaryItems, bool Hidden);

public sealed record TranslationSectionRequest(bool HideWhileDictionaryIsEmpty);

/// <remarks>
/// Reading it takes the same access as the editor: a user who cannot open this package's section
/// gets a refusal, and the backoffice then leaves Umbraco's Translation section alone for them.
/// </remarks>
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = Constants.PackageName)]
public sealed class TranslationSectionController(
    TranslationSectionSettingsStore settings,
    DictionaryPresence dictionary) : TranslationsApiControllerBase
{
    [HttpGet("translation-section")]
    [ProducesResponseType(typeof(TranslationSectionResponse), StatusCodes.Status200OK)]
    public Task<TranslationSectionResponse> Get() => ResponseAsync(settings.Get());

    [HttpPut("translation-section")]
    [Authorize(Policy = TranslationPolicies.ManageSources)]
    [ProducesResponseType(typeof(TranslationSectionResponse), StatusCodes.Status200OK)]
    public Task<TranslationSectionResponse> Save(TranslationSectionRequest request)
    {
        var next = new TranslationSectionSettings(request.HideWhileDictionaryIsEmpty);
        settings.Save(next);
        return ResponseAsync(next);
    }

    private async Task<TranslationSectionResponse> ResponseAsync(TranslationSectionSettings current)
    {
        var hasDictionaryItems = await dictionary.HasItemsAsync();
        return new(current.HideWhileDictionaryIsEmpty, hasDictionaryItems, current.HidesSection(hasDictionaryItems));
    }
}
