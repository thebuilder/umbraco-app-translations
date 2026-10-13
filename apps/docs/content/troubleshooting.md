---
title: Troubleshooting
description: The problems people run into first with App Translations, and what fixes them.
seo:
  image: /og/troubleshooting.png
---

## The delivery endpoint returns `{}`

The locale or namespace does not match what Umbraco stores.

- The locale is Umbraco's code, such as `da-DK`, and has to match exactly. A request for `da` finds nothing.
- The namespace is the one shown before the dot in the editor, such as `website` in `website.greeting`.

**Settings > App translations** lists the exact URL for every language and namespace that has messages.

## An edit does not show up in the application

Clients and CDNs may keep delivery responses for 60 seconds, and your application may cache on top of that. Wait a minute, set a shorter [lifetime](/delivery-api#caching), or subscribe to the [App translations updated webhook](/syncing#telling-your-application-something-changed) and drop the cache when it fires. Also check that the application asks `overrides` or `all` for the language you edited.

## The delivery endpoint returns `401`

The site has a [delivery key](/delivery-api#access). Send it in an `Api-Key` header, or as a bearer token.

## The delivery endpoint returns `409`

- **The namespace mixes syntaxes.** Umbraco never converts between ICU and i18next, so a namespace with both cannot be served in either format. Give each syntax its own source and namespace.
- **Two sources supply the same key.** Give them different namespaces, or remove the key from one.

## A sync fails

**History** next to the source says why. The common causes:

- **Umbraco cannot reach the URL.** Check it from the Umbraco server rather than your own machine. Sources do not follow redirects, so use the final URL.
- **The source is on a private network or `localhost`.** That is blocked unless you [allow private networks](/sources#sources-on-a-private-network).
- **The response is too large or too slow.** Raise the limits under the source's **Advanced** settings.
- **A file is not what the source expects.** Check the catalogue format and message syntax against what the application writes.
- **A language is missing.** Every ticked language needs a file. The error names the one that failed.

**Test source** reads the files with the current settings without saving anything, so you can try a fix first.

## Deployment sync returns `409` or `503`

- `409`: the source is turned off, or another sync of it is running.
- `503`: no deployment key is configured, so the endpoint is off. See [Syncing from a deployment](/syncing#syncing-from-a-deployment).

## Nobody can see the App Translations section

The section is open only to user groups that have it. Add it to a group in **Users > User groups**.

## Umbraco's Translation section disappeared

The package hides it while the site has no Dictionary items. Turn off **Hide while the Dictionary is empty** in **Settings > App translations** to bring it back. See [Umbraco's Translation section](/editing#umbracos-translation-section).
