using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Extensions.DependencyInjection;
using TheBuilder.AppTranslations.Core.Assistant;
using TheBuilder.AppTranslations.Assistant;

namespace TheBuilder.AppTranslations.Tests;

public sealed class AssistantChatRegistrationTests
{
    [Theory]
    [InlineData("17.5.0.0", true)]
    [InlineData("17.7.1.0", true)]
    [InlineData("18.3.0.0", true)]
    [InlineData("1.14.0.0", false)]
    [InlineData("17.4.0.0", false)]
    [InlineData("19.0.0.0", false)]
    public void Only_tested_Umbraco_AI_versions_are_used(string version, bool supported) =>
        Assert.Equal(supported, AssistantChatRegistration.SupportedVersion(Version.Parse(version)));

    [Fact]
    public void A_site_without_Umbraco_AI_gets_the_stand_in()
    {
        // This test project does not reference Umbraco.AI, like a site without a provider package.
        var services = new ServiceCollection();
        services.AddAssistantChat();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var chat = scope.ServiceProvider.GetRequiredService<IAssistantChat>();

        Assert.False(chat.IsInstalled);
    }

    [Fact]
    public void The_adapter_is_found_by_the_name_the_registration_asks_for()
    {
        // Read from the adapter's metadata rather than loaded, so the other tests still run as a
        // site without Umbraco.AI. A rename would otherwise only show as an unavailable assistant.
        var path = Path.Combine(
            AppContext.BaseDirectory,
            AssistantChatRegistration.AdapterAssembly,
            $"{AssistantChatRegistration.AdapterAssembly}.dll");
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();

        var publicTypes = metadata.TypeDefinitions
            .Select(metadata.GetTypeDefinition)
            .Where(type => (type.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public)
            .Select(type => $"{metadata.GetString(type.Namespace)}.{metadata.GetString(type.Name)}");

        Assert.Contains(AssistantChatRegistration.AdapterType, publicTypes);
    }
}
