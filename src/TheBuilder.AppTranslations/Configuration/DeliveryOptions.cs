namespace TheBuilder.AppTranslations.Configuration;

/// <summary>
/// The delivery endpoints. They are public until <see cref="ApiKey"/> is set, which is right for
/// most sites: the text ends up in the application's bundle anyway. A key keeps unreleased copy,
/// or a site's dictionaries, to the applications that hold it.
/// </summary>
public sealed class DeliveryOptions
{
    public const string SectionName = "TheBuilder:AppTranslations:Delivery";
    public const string ApiKeyConfigurationName = $"{SectionName}:ApiKey";

    public string? ApiKey { get; set; }
}
