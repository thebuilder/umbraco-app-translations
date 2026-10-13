using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using TheBuilder.AppTranslations.Core.Assistant;

namespace TheBuilder.AppTranslations.Assistant;

/// <summary>
/// Chooses what answers for <see cref="IAssistantChat"/>: the Umbraco.AI adapter when the site has a
/// version the assistant was built and tested against, and <see cref="UnavailableAssistantChat"/>
/// otherwise.
/// </summary>
/// <remarks>
/// The package does not depend on Umbraco.AI, so it may simply not be there. At startup Umbraco
/// loads every assembly in the site's folder and everything each one references, and stops the site
/// if a reference is missing. The adapter references Umbraco.AI, so the package copies it to a
/// folder of its own, out of Umbraco's sight, and it is loaded from there only once
/// <see cref="SupportedVersion"/> has found a Umbraco.AI it can run against.
/// </remarks>
internal static class AssistantChatRegistration
{
    private const string ChatServiceType = "Umbraco.AI.Core.Chat.IAIChatService, Umbraco.AI.Core";
    private const string AdapterAssembly = "TheBuilder.AppTranslations.UmbracoAI";
    private const string AdapterType = "TheBuilder.AppTranslations.UmbracoAI.UmbracoAiAssistantChat";

    /// <summary>The oldest Umbraco.AI with the members the assistant calls.</summary>
    internal static readonly Version Minimum = new(17, 5);

    /// <summary>
    /// The first major the assistant has not been tested against. Each Umbraco.AI major follows an
    /// Umbraco major, so this moves with the package's own Umbraco range.
    /// </summary>
    internal const int FirstUntestedMajor = 19;

    public static void AddAssistantChat(this IServiceCollection services)
    {
        var adapter = SupportedVersion(Type.GetType(ChatServiceType, throwOnError: false)?.Assembly.GetName().Version)
            ? LoadAdapter()?.GetType(AdapterType, throwOnError: false)
            : null;
        if (adapter is null)
        {
            services.AddScoped<IAssistantChat, UnavailableAssistantChat>();
            return;
        }

        // Umbraco.AI's assemblies can be present while its services are not, for example when the
        // site references Umbraco.AI.Core alone. Creating the adapter then fails, and the assistant
        // reports itself unavailable rather than breaking the settings page.
        services.AddScoped(provider =>
        {
            try
            {
                return (IAssistantChat)ActivatorUtilities.CreateInstance(provider, adapter);
            }
            catch (InvalidOperationException)
            {
                return new UnavailableAssistantChat();
            }
        });
    }

    internal static bool SupportedVersion(Version? version) =>
        version is not null && version >= Minimum && version.Major < FirstUntestedMajor;

    /// <summary>
    /// The adapter from its folder, or null when the site's output does not have it or it cannot be
    /// loaded, so a damaged file turns the assistant off rather than stopping the site. Loaded into
    /// the default context, so it shares this assembly's <see cref="IAssistantChat"/> and the site's
    /// Umbraco.AI, and only once: loading the same file into that context twice fails.
    /// </summary>
    private static Assembly? LoadAdapter()
    {
        var loaded = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(assembly =>
            assembly.GetName().Name == AdapterAssembly);
        if (loaded is not null)
        {
            return loaded;
        }

        var path = Path.Combine(AppContext.BaseDirectory, AdapterAssembly, $"{AdapterAssembly}.dll");
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
        }
        catch (Exception exception) when (exception is BadImageFormatException or FileLoadException)
        {
            return null;
        }
    }
}
