using System.Text.Json.Serialization;
using TheBuilder.AppTranslations.Configuration;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.AppTranslations.Assistant;

/// <summary>
/// How the translation assistant is set up on this site: whether editors see it, which Umbraco.AI
/// chat profile it uses (the site's default when none is chosen), and the instructions it is given.
/// The profile is where the model, the brand voice and the guardrails come from; the instructions
/// are only about translating, and are left empty to use <see cref="AssistantPrompts.DefaultInstructions"/>.
/// </summary>
internal sealed record AssistantSettings(bool Enabled, Guid? ProfileId, string? Instructions)
{
    /// <summary>
    /// Off until an administrator turns it on: it sends the site's text to an AI provider, and
    /// installing or upgrading this package is not agreeing to that.
    /// </summary>
    public static AssistantSettings Default { get; } = new(Enabled: false, ProfileId: null, Instructions: null);

    /// <summary>The instructions actually sent: the site's own, or the default when it has none.</summary>
    /// <remarks>Not stored: a copy of the default would stop following the default when it changes.</remarks>
    [JsonIgnore]
    public string EffectiveInstructions =>
        string.IsNullOrWhiteSpace(Instructions) ? AssistantPrompts.DefaultInstructions : Instructions;
}

/// <summary>An unset or unreadable value turns the assistant off, the safe way to be wrong.</summary>
internal sealed class AssistantSettingsStore(IKeyValueService keyValues)
    : KeyValueSettingsStore<AssistantSettings>(keyValues, Constants.PackageName + ".Assistant", AssistantSettings.Default);
