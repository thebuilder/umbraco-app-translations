using System.Text.Json;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.AppTranslations.Configuration;

/// <summary>
/// One small settings record kept as JSON in Umbraco's own key-value store: a record edited from the
/// settings dashboard needs no table of its own, and the store already travels with the database the
/// rest of the site uses.
/// </summary>
/// <param name="fallback">What an unset or unreadable value means, which each store chooses to be safe.</param>
public abstract class KeyValueSettingsStore<T>(IKeyValueService keyValues, string key, T fallback)
    where T : class
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public T Get()
    {
        var stored = keyValues.GetValue(key);
        if (string.IsNullOrEmpty(stored))
            return fallback;

        try
        {
            return JsonSerializer.Deserialize<T>(stored, Json) ?? fallback;
        }
        catch (JsonException)
        {
            // Something else wrote the key, or a later version did and this one is reading it back.
            // The fallback is safer than guessing what was meant.
            return fallback;
        }
    }

    public void Save(T settings) =>
        keyValues.SetValue(key, JsonSerializer.Serialize(settings, Json));
}
