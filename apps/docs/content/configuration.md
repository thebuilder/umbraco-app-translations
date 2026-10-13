---
title: Configuration
description: The app settings App Translations reads, and how source headers get their values.
seo:
  image: /og/configuration.png
---

App Translations reads its settings from the `TheBuilder:AppTranslations` section of your site's configuration. As environment variables, the separators are double underscores, for example `TheBuilder__AppTranslations__DeploymentSync__ApiKey`.

| Setting | Default | Purpose |
| --- | --- | --- |
| `TheBuilder:AppTranslations:SourceSecurity:AllowPrivateNetworkEndpoints` | `false` | Let sources read from private networks and `localhost`. |
| `TheBuilder:AppTranslations:DeploymentSync:ApiKey` | none | Turns on [deployment-triggered sync](/syncing#syncing-from-a-deployment). A long random value, without spaces. |
| `TheBuilder:AppTranslations:Delivery:ApiKey` | none | Requires a key for the [delivery endpoints](/delivery-api#access), which are public without one. A long random value, without spaces. |

A key that contains whitespace stops the site at startup, because it could never match what a client sends.

A source header can name a setting to read its value from, which keeps secrets out of the database. See [Sources that need authentication](/sources#sources-that-need-authentication).

Everything else is set in the backoffice, in **Settings > App translations**: sources, the AI assistant, and whether Umbraco's Translation section is hidden while the Dictionary is empty.

One Umbraco setting matters too. Umbraco sends the [App translations updated webhook](/syncing#telling-your-application-something-changed) only while `Umbraco:CMS:Webhook:Enabled` is on, which it is by default.
