using TheBuilder.AppTranslations.Core.Messages;
using TheBuilder.AppTranslations.Core.Output;
using TheBuilder.AppTranslations.Core.Persistence;
using TheBuilder.AppTranslations.DeliveryApi;

namespace TheBuilder.AppTranslations.Tests;

public sealed class DeliveryOutputTests
{
    [Fact]
    public void One_namespace_is_the_serializer_output_as_it_is()
    {
        var json = DeliveryOutput.Serialize(new NestedJsonOutputSerializer(), [View("website", "navigation.home", "Home")],
            TranslationOutputMode.All, DeliveryNamespaces.From(["website"]));

        Assert.Equal("{\"navigation\":{\"home\":\"Home\"}}", json);
    }

    [Fact]
    public void Several_namespaces_each_sit_under_their_own_name()
    {
        var json = DeliveryOutput.Serialize(new NestedJsonOutputSerializer(),
            [View("navigation", "title", "Menu"), View("checkout", "title", "Payment"), View("site.v2", "home", "Home")],
            TranslationOutputMode.All, DeliveryNamespaces.From(null));

        // A dot in a namespace's name is part of the name, not a path into it.
        Assert.Equal("{\"checkout\":{\"title\":\"Payment\"},\"navigation\":{\"title\":\"Menu\"},\"site.v2\":{\"home\":\"Home\"}}", json);
    }

    [Fact]
    public void Overrides_leave_out_a_namespace_nobody_edited()
    {
        var json = DeliveryOutput.Serialize(new NestedJsonOutputSerializer(),
            [View("checkout", "title", "Payment", "Betaling"), View("navigation", "title", "Menu")],
            TranslationOutputMode.Overrides, DeliveryNamespaces.From(null));

        Assert.Equal("{\"checkout\":{\"title\":\"Betaling\"}}", json);
    }

    [Fact]
    public void A_namespace_name_is_escaped_as_json()
    {
        var json = DeliveryOutput.Serialize(new NestedJsonOutputSerializer(),
            [View("say \"hi\"", "title", "Hi"), View("other", "title", "Other")],
            TranslationOutputMode.All, DeliveryNamespaces.From(null));

        Assert.Equal("Hi", System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("say \"hi\"").GetProperty("title").GetString());
    }

    private static TranslationMessageView View(string messageNamespace, string key, string defaultValue, string? overrideValue = null)
    {
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        var message = new TranslationMessage(id, new(Guid.NewGuid(), messageNamespace, key, "da"), defaultValue, MessageFormat.Icu,
            new Dictionary<string, string>(), null, "r1", "checksum", null, now, now, TranslationMessageState.Active);
        return new(message, overrideValue is null ? null : new TranslationOverride(id, overrideValue, "checksum", 1, now, "test", now, "test"));
    }
}
