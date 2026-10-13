using Umbraco.Cms.Core.Services;

namespace TheBuilder.AppTranslations.Tests;

/// <summary>Umbraco's key-value store, in memory.</summary>
internal sealed class FakeKeyValues : IKeyValueService
{
    private readonly Dictionary<string, string?> _values = [];

    public string? GetValue(string key) => _values.GetValueOrDefault(key);

    public IReadOnlyDictionary<string, string?>? FindByKeyPrefix(string keyPrefix) =>
        _values.Where(pair => pair.Key.StartsWith(keyPrefix, StringComparison.Ordinal)).ToDictionary();

    public void SetValue(string key, string value) => _values[key] = value;

    public void SetValue(string key, string originValue, string newValue) => _values[key] = newValue;

    public bool TrySetValue(string key, string originValue, string newValue)
    {
        _values[key] = newValue;
        return true;
    }
}
