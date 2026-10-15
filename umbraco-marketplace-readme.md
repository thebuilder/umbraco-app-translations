![The App Translations editor in the Umbraco backoffice](https://raw.githubusercontent.com/thebuilder/umbraco-app-translations/refs/heads/main/apps/docs/content/screenshots/editor.png)

App Translations lets editors change the text in your application from the Umbraco backoffice, without a code change or a deploy.

Your application keeps its own translation files. Umbraco reads them, editors change the messages that need changing, and your application fetches those changes and lays them over its own text.

## What it does

- Reads nested JSON, as [next-intl](https://next-intl.dev) and [i18next](https://www.i18next.com) write it, and next-intl's gettext PO files, from any URL your site can reach.
- Checks every edit against the message syntax, ICU, i18next JSON v4 or plain text, so an edit cannot break a placeholder or a plural.
- Lists every message with search and filters, next to a language to compare against, and records who last changed each one, and when.
- Keeps custom text through every sync. When the application changes a default, the custom text stays, marked so an editor can check it still fits.
- Serves the custom text, or the complete dictionary, from a delivery endpoint your application merges over the messages it bundles.
- Translates into languages the application does not ship, too.
- With [Umbraco.AI](https://docs.umbraco.com/umbraco-ai) set up, translates a message from another language, or improves, simplifies or shortens it, in your brand voice. Umbraco.AI is optional.

## Install

Needs Umbraco CMS 17.1 through 18.x on .NET 10, and translation files your Umbraco site can fetch over HTTP, one file per language.

```sh
dotnet add package TheBuilder.AppTranslations
```

The package creates its tables and opens its section on the first start, so there is nothing else to wire up.

## Get started

1. In Umbraco, go to **Settings > App translations** and add a source: the URL of your files, such as `https://app.example.com/messages/{locale}.json`, the languages to read, and the message syntax.
2. Select **Test source**, then **Sync now**.
3. Open the **App Translations** section, find a message, and write custom text for it. **Save** makes it live.
4. Point your application at the delivery endpoint, and merge the custom text over its own:

   ```http
   GET /umbraco/delivery/api/v1/app-translations/overrides?locale=da-DK&namespace=website
   ```

## Documentation

[app-translations.thebuilder.dk](https://app-translations.thebuilder.dk/) has the quickstart, the source settings, complete next-intl and i18next examples, syncing from a deployment, and troubleshooting.
