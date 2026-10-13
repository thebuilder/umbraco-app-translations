using Microsoft.Extensions.AI;
using TheBuilder.AppTranslations.Core.Assistant;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;

namespace TheBuilder.AppTranslations.UmbracoAI;

/// <summary>
/// The assistant's chat on Umbraco.AI. Created by name, and only on a site that has a supported
/// Umbraco.AI; see AssistantChatRegistration in the main assembly.
/// </summary>
public sealed class UmbracoAiAssistantChat(IAIChatService chat, IAIProfileService profiles) : IAssistantChat
{
    /// <summary>What the requests are called in Umbraco.AI's own usage and audit logs.</summary>
    private const string RequestAlias = "theBuilderAppTranslations";

    public bool IsInstalled => true;

    public async Task<IReadOnlyList<AssistantProfile>> GetProfilesAsync(CancellationToken cancellationToken) =>
        (await profiles.GetProfilesAsync(AICapability.Chat, cancellationToken))
            .Select(profile => new AssistantProfile(profile.Id, profile.Alias, profile.Name))
            .OrderBy(profile => profile.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

    public Task<bool> HasDefaultProfileAsync(CancellationToken cancellationToken) =>
        profiles.HasDefaultProfileAsync(AICapability.Chat, cancellationToken);

    public async Task<bool> ProfileExistsAsync(Guid id, CancellationToken cancellationToken) =>
        await profiles.GetProfileAsync(id, cancellationToken) is { Capability: AICapability.Chat };

    public async Task<string> CompleteAsync(
        Guid? profileId,
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var response = await chat.GetChatResponseAsync(
            builder =>
            {
                builder.WithAlias(RequestAlias).WithName("App Translations");
                if (profileId is { } id)
                    builder.WithProfile(id);
            },
            messages,
            cancellationToken);
        return response.Text;
    }
}
