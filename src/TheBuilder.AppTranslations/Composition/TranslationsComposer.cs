using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TheBuilder.AppTranslations.Assistant;
using TheBuilder.AppTranslations.Authorization;
using TheBuilder.AppTranslations.Configuration;
using TheBuilder.AppTranslations.Core.Output;
using TheBuilder.AppTranslations.Core.Persistence;
using TheBuilder.AppTranslations.Core.Sources;
using TheBuilder.AppTranslations.Core.Synchronization;
using TheBuilder.AppTranslations.Core.Validation;
using TheBuilder.AppTranslations.DeliveryApi;
using TheBuilder.AppTranslations.Localization;
using TheBuilder.AppTranslations.Migrations;
using TheBuilder.AppTranslations.Notifications;
using TheBuilder.AppTranslations.Persistence;
using TheBuilder.AppTranslations.Webhooks;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace TheBuilder.AppTranslations.Composition;

public sealed class TranslationsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.PackageMigrationPlans().Add(typeof(TranslationsMigrationPlan));
        builder.Services.AddOptions<DeploymentSyncOptions>()
            .Bind(builder.Config.GetSection(DeploymentSyncOptions.SectionName))
            .Validate(options => ApiKey.IsUsable(options.ApiKey), $"{DeploymentSyncOptions.ApiKeyConfigurationName} must not contain whitespace.")
            .ValidateOnStart();
        builder.Services.AddOptions<DeliveryOptions>()
            .Bind(builder.Config.GetSection(DeliveryOptions.SectionName))
            .Validate(options => ApiKey.IsUsable(options.ApiKey), $"{DeliveryOptions.ApiKeyConfigurationName} must not contain whitespace.")
            .Validate(options => options.MaxAgeSeconds >= 0, $"{DeliveryOptions.MaxAgeSecondsConfigurationName} must be 0 or more.")
            .ValidateOnStart();
        builder.Services.AddOptions<TranslationSourceSecurityOptions>()
            .Bind(builder.Config.GetSection(TranslationSourceSecurityOptions.SectionName));
        builder.Services.AddAuthorization(ConfigurePolicies);
        builder.Services.AddSingleton<DeploymentSyncApiKeyAuthenticator>();
        builder.Services.AddSingleton<DeliveryApiKeyAuthenticator>();
        builder.Services.AddSingleton<DeliveryResponseCache>();
        builder.CacheRefreshers().Add<DeliveryResponseCacheRefresher>();
        builder.AddNotificationHandler<TranslationsUpdatedNotification, DeliveryResponseCacheInvalidator>();
        builder.Services.AddSingleton<IAuthorizationHandler, BackofficeAdministratorHandler>();
        builder.Services.AddSingleton<IAuthorizationHandler, AppTranslationsSectionAccessHandler>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IMessageFormatValidator, MessageFormatValidator>();
        // The concrete parsers are registered as themselves; only the one that chooses between them
        // answers to the interface, so nothing downstream has to know a choice was made.
        builder.Services.AddSingleton<NestedJsonParser>();
        builder.Services.AddSingleton<PoParser>();
        builder.Services.AddSingleton<ITranslationSourceParser, TranslationCatalogParser>();
        builder.Services.AddHttpClient<ITranslationSourceTransport, HttpTranslationSourceTransport>()
            .ConfigurePrimaryHttpMessageHandler(services => TranslationSourceHttpHandler.Create(
                services.GetRequiredService<IOptions<TranslationSourceSecurityOptions>>().Value));
        builder.AddWebhookEvent<TranslationsUpdatedWebhookEvent>();
        builder.Services.AddScoped<UmbracoTranslationStore>();
        builder.Services.AddScoped<ITranslationSourceRepository>(services => services.GetRequiredService<UmbracoTranslationStore>());
        builder.Services.AddScoped<ITranslationSynchronizationStore>(services => services.GetRequiredService<UmbracoTranslationStore>());
        builder.Services.AddScoped<ITranslationMessageRepository>(services => services.GetRequiredService<UmbracoTranslationStore>());
        builder.Services.AddScoped<ITranslationEditorRepository>(services => services.GetRequiredService<UmbracoTranslationStore>());
        builder.Services.AddScoped<TranslationSyncEngine>();
        builder.Services.AddSingleton<ITranslationOutputSerializer, NestedJsonOutputSerializer>();
        // No caching wrapper: ILanguageService is already cached inside Umbraco, and a second layer
        // would only introduce staleness after a language is added or the default one changes.
        builder.Services.AddScoped<ITranslationLocaleCatalog, UmbracoTranslationLocaleCatalog>();
        // The assistant runs on Umbraco.AI, which the package depends on, so its services are always
        // there; whether the assistant is offered is a setting, and a profile, rather than an install.
        builder.Services.AddScoped<IAssistantChat, UmbracoAiAssistantChat>();
        builder.Services.AddScoped<AssistantSettingsStore>();
        builder.Services.AddScoped<TranslationAssistant>();
        builder.Services.AddScoped<TranslationSectionSettingsStore>();
        builder.Services.AddScoped<DictionaryPresence>();
        builder.AddNotificationHandler<DictionaryCacheRefresherNotification, DictionaryPresenceInvalidator>();
    }

    private static void ConfigurePolicies(AuthorizationOptions options)
    {
        options.AddPolicy(TranslationPolicies.View, policy => policy.RequireAuthenticatedUser().AddRequirements(new AppTranslationsSectionAccessRequirement()));
        options.AddPolicy(TranslationPolicies.Edit, policy => policy.RequireAuthenticatedUser().AddRequirements(new AppTranslationsSectionAccessRequirement()));
        options.AddPolicy(TranslationPolicies.ManageSources, policy => policy.RequireAuthenticatedUser().AddRequirements(new BackofficeAdministratorRequirement()));
        options.AddPolicy(TranslationPolicies.Sync, policy => policy.RequireAuthenticatedUser().AddRequirements(new BackofficeAdministratorRequirement()));
    }
}
