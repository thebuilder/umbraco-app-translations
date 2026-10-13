namespace TheBuilder.AppTranslations.Core.Sources;

public interface ITranslationSourceTransport
{
    string Kind { get; }

    Task<TranslationSourcePayload> FetchAsync(
        TranslationSourceDefinition source,
        TranslationFetchContext context,
        CancellationToken cancellationToken);
}
