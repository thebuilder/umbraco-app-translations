using TheBuilder.AppTranslations.Core.Persistence;

namespace TheBuilder.AppTranslations.Core.Output;

public interface ITranslationOutputSerializer
{
    string Serialize(IReadOnlyCollection<TranslationMessageView> messages, TranslationOutputMode mode);
}

public enum TranslationOutputMode
{
    Overrides,
    All,
}
