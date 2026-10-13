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
    public const string MaxAgeSecondsConfigurationName = $"{SectionName}:MaxAgeSeconds";

    public string? ApiKey { get; set; }

    /// <summary>
    /// How long a client or CDN may reuse a response before asking again. Umbraco keeps its own copy
    /// until the translations change, so asking again is cheap: <c>0</c> makes every client
    /// revalidate, and see an edit as soon as it is saved, for the price of a request that usually
    /// ends in 304.
    /// </summary>
    public int MaxAgeSeconds { get; set; } = 60;
}
