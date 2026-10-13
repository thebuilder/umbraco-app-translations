---
title: Delivery API
description: Fetch the custom text for a language and namespace, or the complete dictionary, in next-intl or i18next format.
seo:
  image: /og/delivery-api.png
---

Your application fetches translations from two endpoints. **Settings > App translations** lists the exact URL for each language and namespace.

## Custom text only

To use the custom text, fetch it for a language and namespace, then merge it over the messages your application already bundles.

```http
GET /umbraco/delivery/api/v1/app-translations/overrides?locale=da-DK&namespace=website&format=next-intl
```

## The complete dictionary

If your application does not bundle its own text, fetch the complete dictionary instead. It has the custom text in place of the application's text wherever an editor wrote some.

```http
GET /umbraco/delivery/api/v1/app-translations/all?locale=da-DK&namespace=website&format=next-intl
```

## Caching

Both endpoints answer with an ETag, so a client that sends `If-None-Match` gets `304 Not Modified` until something changes. To drop a cached dictionary the moment it changes, subscribe to the [App translations updated webhook](/syncing#telling-your-application-something-changed).

## i18next

For i18next, use `format=i18next-v4` and set the source's message syntax to **i18next JSON v4**. The output keeps nested keys, `{{variable}}` interpolation, context keys and plural suffixes such as `_one`, `_other` and `_ordinal_one`.

:::note
Umbraco never converts text between ICU and i18next syntax, so keep each namespace to one syntax. Umbraco refuses to serve a namespace that mixes them until you split it.
:::
