using System.Text.Json.Serialization;

namespace TheBuilder.AppTranslations.Core.Messages;

[JsonConverter(typeof(JsonStringEnumConverter<MessageFormat>))]
public enum MessageFormat
{
    PlainText,
    Icu,
    I18NextV4,
}
