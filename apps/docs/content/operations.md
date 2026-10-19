---
title: Operations
description: Where App Translations keeps its data, how it behaves on several servers, its limits, and what upgrading and uninstalling do.
seo:
  image: /og/operations.png
---

## Where the data lives

Everything is in the site's Umbraco database, so it is backed up and restored with it.

| Table | Holds |
| --- | --- |
| `theBuilderAppTranslationSource` | Sources and their settings |
| `theBuilderAppTranslationMessage` | Every message, per language, with the application's text |
| `theBuilderAppTranslationOverride` | Custom text, with who last saved it and when |
| `theBuilderAppTranslationSync` | Sync history |

The AI assistant settings and the Translation section setting are in Umbraco's key-value store. A source header stores its value only when you type one in; a header that names a setting reads it from your configuration instead.

## Several servers

- **Syncing.** A source syncs on one server at a time. Each sync holds a lease on its source, and a second sync that starts meanwhile gets `409`. A lease left by a server that stopped expires on its own.
- **Scheduled syncs.** Only the Single or SchedulingPublisher server runs the [schedule](/syncing#syncing-on-a-schedule).
- **Webhooks.** Umbraco sends them only from the Single or SchedulingPublisher server.
- **Delivery.** Every server keeps its own copy of each response, built from the shared database, with the same ETag for the same text. A change clears the copies on every server, through Umbraco's distributed cache.

## Limits

- A source's request times out after 10 seconds, and its response can be at most 10 MB. Both are set per source, under **Advanced**.
- Sources connect only to public addresses, do not follow redirects and do not use the host's proxy, unless you [allow private networks](/sources#sources-on-a-private-network).
- Clients and CDNs may keep delivery responses for 60 seconds, unless you [change it](/delivery-api#caching).

## Upgrading

Upgrade the NuGet package and restart the site. Any database changes run on startup, as Umbraco package migrations.

## Uninstalling

Remove the package and restart the site.

- The App Translations section and its settings go away, and Umbraco's own Translation section comes back if the package was hiding it.
- Umbraco does not delete a package's tables, so the four tables above, and everything in them, stay in the database. Reinstalling picks up where it left off. To remove the data for good, drop the tables yourself.
- User groups keep the App Translations section in their list of allowed sections. Without the package it does nothing.
- Your application keeps working on its own text. Requests to the delivery endpoints get `404`, so make sure the application falls back, as the [examples](/delivery-api#next-intl) do.
