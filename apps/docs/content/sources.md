---
title: Sources
description: Connect Umbraco to your application's translation files, choose the message syntax, and read from authenticated or private sources.
seo:
  image: /og/sources.png
---

A source is one set of translation files. Add and manage sources in **Settings > App translations**.

## The URL

A placeholder in the source's URL names the language to fetch:

```text
https://app.example.com/messages/{locale}.json     ->  .../messages/en-US.json
https://app.example.com/messages/{language}.json   ->  .../messages/en.json
https://app.example.com/{language}/{locale}.json   ->  .../en/en-US.json
```

Umbraco's language codes carry a region, such as `en-US`, but many applications name their files by language alone, such as `en`. Use `{language}` when yours do. Umbraco still stores each message under the full code, so `en-US` and `en-GB` stay separate.

## Settings

![A source's settings: its name, the URL with a {locale} placeholder, request headers, the Umbraco languages it supplies, the catalogue format, namespace and message syntax](./screenshots/source-editor.png)

- **Message syntax.** ICU messages, as next-intl and FormatJS use them, i18next JSON v4, or plain text. The editor checks every change against the syntax, so an edit cannot break a placeholder or a plural.
- **Namespace handling.** Use the first key of the JSON as the namespace, or put every message in one namespace you name. The namespace is part of the delivery URL.
- **Advanced.** The source identifier for [deployment-triggered sync](/syncing#syncing-from-a-deployment), a request timeout, and the largest response the source accepts.

**Test source** fetches the files with the current settings without saving anything. **Sync now** reads them in.

## Sources that need authentication

You can add request headers to a source, but Umbraco never stores their values. Each header names a configuration setting instead, and your site's configuration supplies the value. This example gives a header `X-Api-Key` the setting name `Translations:SourceApiKey`:

```json
{
  "Translations": {
    "SourceApiKey": "development-value-only"
  }
}
```

In a deployed environment, supply the value as the environment variable `Translations__SourceApiKey` or through your secret store. For a bearer token, add an `Authorization` header and make the setting's value the whole header, for example `Bearer your-token`.

## Sources on a private network

By default, sources connect only to public addresses, do not follow redirects and do not use the host's proxy. That stops anyone from using a source URL to reach internal services.

To read from a private network or `localhost`, turn on `TheBuilder:AppTranslations:SourceSecurity:AllowPrivateNetworkEndpoints`.

:::caution
Only allow private-network sources in development or a network you trust.
:::
