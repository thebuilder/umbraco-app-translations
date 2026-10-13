---
title: Syncing
description: Read new and changed messages from a source, from the backoffice or a deployment pipeline, and tell your application when anything changes.
seo:
  image: /og/syncing.png
---

## Syncing a source

Umbraco reads a source when someone presses **Sync now** in **Settings > App translations**, which also keeps the history of every sync. Sources are not polled on a schedule. To sync as part of a deployment, use the endpoint below.

## Syncing from a deployment

A deployment can sync a source by its identifier, without a backoffice login. The endpoint is off until you configure a secret. Use a long random value; anyone who has it can start a sync.

```sh
TheBuilder__AppTranslations__DeploymentSync__ApiKey=replace-with-a-long-random-value
```

Give your deployment pipeline the same secret, and have it call the endpoint once the new translation files are live.

```sh
curl --fail-with-body \
  --request POST \
  --header "Authorization: Bearer $UMBRACO_TRANSLATIONS_SYNC_KEY" \
  "$UMBRACO_URL/umbraco/app-translations/api/v1/sources/website/sync"
```

Replace `website` with the source identifier from the source's **Advanced** settings. The request waits for the sync to finish and returns how many messages it added, changed and removed.

| Response | Meaning |
| --- | --- |
| `200` | Synced. |
| `401` | The secret is missing or wrong. |
| `404` | No source has that identifier. |
| `409` | The source is turned off, or already syncing. |
| `502` | Umbraco could not fetch or read the translation files. |
| `503` | No secret is configured, so the endpoint is off. |

:::caution
Use HTTPS, and keep the secret in the secret stores on both sides.
:::

## Telling your application something changed

To drop a cached dictionary the moment it changes, add a webhook in **Settings > Webhooks** for the **App translations updated** event (under **Other**). Umbraco handles the URL, headers, retries and the request log as it does for any webhook.

The event fires once for every change that reaches the delivery endpoints.

| `change` | When |
| --- | --- |
| `Synchronized` | A sync added, changed or removed at least one message. |
| `OverrideSaved` | An editor saved custom text. |
| `OverrideRemoved` | An editor reverted custom text to the default. |
| `SourceDeleted` | Someone deleted a source, and everything it provided. |

```json
{
  "sourceId": "6f2f5d3e-1f4a-4a1e-9a5b-2c7d8e9f0a1b",
  "sourceAlias": "website",
  "change": "OverrideSaved",
  "occurredAt": "2026-08-26T09:30:00+00:00",
  "message": { "namespace": "website", "key": "cart.title", "locale": "da-DK" },
  "synchronization": null
}
```

For a saved or reverted message, `message` names it. Its locale and namespace tell you exactly which dictionary to purge. For a sync, `synchronization` holds the revision and the added, changed and removed counts. Umbraco does not announce a sync that found nothing new.

Umbraco sends webhooks only when `Umbraco:CMS:Webhook:Enabled` is on, which it is by default. In a load-balanced setup, Umbraco sends webhooks only from the Single or SchedulingPublisher server.
