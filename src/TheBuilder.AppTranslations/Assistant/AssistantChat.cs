using Microsoft.Extensions.AI;
using TheBuilder.AppTranslations.Core.Assistant;

namespace TheBuilder.AppTranslations.Assistant;

/// <summary>
/// Stands in when the site has no Umbraco.AI the assistant can use. Nothing to choose and nothing
/// to call, so the settings say what to install and the editor does not offer suggestions.
/// </summary>
internal sealed class UnavailableAssistantChat : IAssistantChat
{
    public bool IsInstalled => false;

    public Task<IReadOnlyList<AssistantProfile>> GetProfilesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AssistantProfile>>([]);

    public Task<bool> HasDefaultProfileAsync(CancellationToken cancellationToken) => Task.FromResult(false);

    public Task<bool> ProfileExistsAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(false);

    public Task<string> CompleteAsync(Guid? profileId, IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Umbraco.AI is not installed.");
}
