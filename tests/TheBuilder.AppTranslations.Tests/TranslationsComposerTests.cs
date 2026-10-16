using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TheBuilder.AppTranslations.Composition;
using TheBuilder.AppTranslations.Configuration;
using TheBuilder.AppTranslations.DeliveryApi;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Scoping;

namespace TheBuilder.AppTranslations.Tests;

/// <summary>
/// The registrations themselves, from the real composer. Controller tests build their controllers by
/// hand, so a dependency the composer stopped registering would pass every one of them and only fail
/// on a running site, on the first request to that controller.
/// </summary>
public sealed class TranslationsComposerTests
{
    public static TheoryData<Type> Controllers() =>
        new(typeof(TranslationsComposer).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(ControllerBase)))
            .OrderBy(type => type.FullName, StringComparer.Ordinal));

    [Theory]
    [MemberData(nameof(Controllers))]
    public void Every_controller_can_be_created_from_the_registered_services(Type controller)
    {
        using var provider = Compose().BuildServiceProvider();
        using var scope = provider.CreateScope();

        // How ASP.NET creates a controller: from its public constructor, with every parameter a service.
        Assert.NotNull(ActivatorUtilities.CreateInstance(scope.ServiceProvider, controller));
    }

    [Fact]
    public void The_controller_and_the_cache_refresher_share_one_response_cache()
    {
        using var provider = Compose().BuildServiceProvider();

        // The controller reads the cache through the interface and the refresher clears it through
        // the class. Two instances would leave the controller serving responses nothing ever clears.
        Assert.Same(
            provider.GetRequiredService<DeliveryResponseCache>(),
            provider.GetRequiredService<IDeliveryResponseCache>());
    }

    [Fact]
    public void Checks_the_deployment_sync_policy_when_the_options_are_built()
    {
        // The composer registers the validator; ValidateOnStart then runs it as the site starts.
        var services = Compose(new Dictionary<string, string?>
        {
            [DeploymentSyncOptions.AuthorizationPolicyConfigurationName] = "NotRegistered",
        });
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<DeploymentSyncOptions>>().Value);
        Assert.Contains("NotRegistered", exception.Message, StringComparison.Ordinal);
    }

    private static ServiceCollection Compose(IDictionary<string, string?>? settings = null)
    {
        var services = new ServiceCollection();
        var typeLoader = new TypeLoader(new NoTypes(), NullLogger<TypeLoader>.Instance);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings ?? new Dictionary<string, string?>()).Build();
        var builder = new UmbracoBuilder(services, configuration, typeLoader);
        new TranslationsComposer().Compose(builder);

        // What the host and the rest of Umbraco provide on a running site, where the real services
        // need a database. Nothing here is called while a controller is created, so stand-ins that
        // refuse every call are enough.
        services.AddLogging();
        services.AddAuthentication();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(AppCaches.NoCache);
        services.AddSingleton(StandIn.For<IScopeProvider>());
        services.AddSingleton(StandIn.For<IEventAggregator>());
        services.AddSingleton(StandIn.For<IBackOfficeSecurityAccessor>());
        services.AddSingleton(StandIn.For<IKeyValueService>());
        services.AddSingleton(StandIn.For<IDictionaryItemService>());
        services.AddSingleton(StandIn.For<ILanguageService>());
        services.AddSingleton(StandIn.For<IUserService>());
        return services;
    }

    /// <summary>Nothing to scan: the composer registers its types by name.</summary>
    private sealed class NoTypes : ITypeFinder
    {
        public IEnumerable<Assembly> AssembliesToScan => [];

        public IEnumerable<Type> FindClassesOfType(Type assignTypeFrom, IEnumerable<Assembly>? assemblies = null, bool onlyConcreteClasses = true) => [];

        public IEnumerable<Type> FindClassesOfTypeWithAttribute(Type assignTypeFrom, Type attributeType, IEnumerable<Assembly>? assemblies = null, bool onlyConcreteClasses = true) => [];

        public IEnumerable<Type> FindClassesWithAttribute(Type attributeType, IEnumerable<Assembly>? assemblies = null, bool onlyConcreteClasses = true) => [];

        public Type? GetTypeByName(string name) => null;
    }

    /// <summary>An implementation of any interface that throws when called.</summary>
    public class StandIn : DispatchProxy
    {
        public static T For<T>() where T : class => DispatchProxy.Create<T, StandIn>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"{targetMethod?.DeclaringType?.Name}.{targetMethod?.Name} was called on a stand-in.");
    }
}
