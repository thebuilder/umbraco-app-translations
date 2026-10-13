using Microsoft.Extensions.AI;

namespace TheBuilder.AppTranslations.Core.Assistant;

/// <summary>An Umbraco.AI chat profile, as the settings dashboard offers it.</summary>
public sealed record AssistantProfile(Guid Id, string Alias, string Name);

/// <summary>
/// The four things the assistant asks of Umbraco.AI. A seam rather than the Umbraco.AI services
/// themselves because those are wide (the profile service alone has fifteen members), the
/// assistant's tests need to stand in for a model, and the package does not depend on Umbraco.AI:
/// it lives here, in an assembly with no Umbraco reference, so the adapter that implements it can
/// load on a site that has Umbraco.AI without the rest of the package needing it.
/// </summary>
public interface IAssistantChat
{
    /// <summary>Whether the site has a version of Umbraco.AI the assistant can use.</summary>
    bool IsInstalled { get; }

    Task<IReadOnlyList<AssistantProfile>> GetProfilesAsync(CancellationToken cancellationToken);

    Task<bool> HasDefaultProfileAsync(CancellationToken cancellationToken);

    /// <summary>Whether a chat profile with this id exists.</summary>
    Task<bool> ProfileExistsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Runs the conversation through the given profile, or the site's default chat profile when
    /// none is given, and returns the reply's text. The profile supplies the model and its contexts,
    /// so brand voice and guardrails apply here exactly as they do anywhere else on the site.
    /// </summary>
    Task<string> CompleteAsync(Guid? profileId, IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken);
}
