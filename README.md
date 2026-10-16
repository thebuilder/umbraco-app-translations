<p align="center">
  <img src="https://raw.githubusercontent.com/thebuilder/umbraco-app-translations/refs/heads/main/apps/docs/public/logo-mark.svg" width="112" height="112" alt="App Translations logo" />
</p>

<h1 align="center">App Translations for Umbraco</h1>

<p align="center">
  Let editors change the text in your application from the Umbraco backoffice,<br />
  without a code change or a deploy.
</p>

[![NuGet version](https://img.shields.io/nuget/v/TheBuilder.AppTranslations)](https://www.nuget.org/packages/TheBuilder.AppTranslations)
[![NuGet downloads](https://img.shields.io/nuget/dt/TheBuilder.AppTranslations)](https://www.nuget.org/packages/TheBuilder.AppTranslations)
[![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-3544b1)](https://marketplace.umbraco.com/package/thebuilder.apptranslations)
[![License](https://img.shields.io/github/license/thebuilder/umbraco-app-translations)](https://github.com/thebuilder/umbraco-app-translations/blob/main/LICENSE)

Your application keeps its own translation files. Umbraco reads them, editors change the messages
that need changing, and your application fetches those changes and lays them over its own text.

- **The application owns the messages.** Keys, the default text, the message syntax and the
  placeholders all come from the application's files, and stay there.
- **Umbraco owns the edits.** Editors write custom text for any message in any language, and
  Umbraco records who last changed each one, and when.
- **The application decides what to show.** It merges the custom text over its own, or asks
  Umbraco for the complete dictionary.

A site can also translate into languages the application does not ship. Umbraco serves text
written for such a language as the whole message, because the application has nothing to merge it
over.

The package supports Umbraco 17.1 and later, Umbraco 18 included, on .NET 10.

## Getting started

1. Install the package in your Umbraco site:

   ```sh
   dotnet add package TheBuilder.AppTranslations
   ```

2. Make your application's translation files reachable over HTTP, one file per language. The
   package reads nested JSON, as next-intl and i18next write it, and next-intl's gettext PO files.

3. In Umbraco, go to **Settings > App translations** and add a source. A source holds the URL of the
   files, the languages to read, and the message syntax your application uses. **Test source** fetches it
   without saving anything; **Sync now** reads it in.

4. Open the **App Translations** section and start editing.

5. Point your application at the delivery endpoint, as described in
   [Using the translations in your application](#using-the-translations-in-your-application).

## Adding a source

A source is one set of translation files. A placeholder in its URL names the language to fetch:

```
https://app.example.com/messages/{locale}.json     ->  .../messages/en-US.json
https://app.example.com/messages/{language}.json   ->  .../messages/en.json
https://app.example.com/{language}/{locale}.json   ->  .../en/en-US.json
```

Umbraco's language codes carry a region, such as `en-US`, but many applications name their files by
language alone, such as `en`. Use `{language}` when yours do. Umbraco still stores each message
under the full code, so `en-US` and `en-GB` stay separate.

A source has three more settings.

- **Message syntax.** ICU messages, as next-intl and FormatJS use them, i18next JSON v4, or plain
  text. The editor checks every change against the syntax, so an edit cannot break a placeholder or
  a plural.
- **Namespace handling.** Use the first key of the JSON as the namespace, or put every message in
  one namespace you name. Your application fetches one namespace at a time, or all of them at once.
- **Advanced.** The source identifier for deployment-triggered sync, a request timeout, and the
  largest response the source accepts.

### Sources that need authentication

You can add request headers to a source. Each header either holds its value, or names a
configuration setting that supplies the value when Umbraco makes the request. A typed value is
stored in the database in plain text, so put secrets in a setting. This example gives a header
`X-Api-Key` the setting name `Translations:SourceApiKey`:

```json
{
  "Translations": {
    "SourceApiKey": "development-value-only"
  }
}
```

In a deployed environment, supply the value as the environment variable `Translations__SourceApiKey`
or through your secret store. For a bearer token, add an `Authorization` header and make the setting's
value the whole header, for example `Bearer your-token`.

### Sources on a private network

By default, sources connect only to public addresses, do not follow redirects and do not use the
host's proxy. That stops anyone from using a source URL to reach internal services. To read from a private
network or `localhost`, turn on `AppTranslations:SourceSecurity:AllowPrivateNetworkEndpoints`.
Only do that in development or a network you trust.

## Editing translations

The **App Translations** section lists every message, one row per key. Choose the language you are
editing and, if you like, a language to compare against. The ⇄ button between them swaps the two.
The editor remembers the pair for the rest of the browser session.

- Search finds text in any language, and keys.
- Filters narrow the list by namespace and by status. The statuses are not written, custom text,
  default text, default changed, and no longer used.
- Click a row to edit it in place. The field highlights placeholders, and a message with plurals or
  choices shows an example of each case under the field.
- **Save** saves the row. **Save & next** saves it and opens the next one, for working down a list.
  The bottom of the open row lists the keyboard shortcuts.
- Opening another row or closing this one keeps your unsaved text. The row shows it, marked as
  unsaved, with its own **Save** and **Discard**. **Save all** at the top saves every unsaved row at
  once. Unsaved text survives a visit to another section or a reload.
- **Revert to default** removes the custom text and returns to what the application ships.
- **Default changed** marks custom text whose application default has changed since someone wrote
  it, so you can check it still fits.

Anyone with access to the App Translations section can edit. Administrators manage sources and the
AI settings.

### Who can open the section

The package installs its own **App Translations** section and opens it to the user groups that can
open Umbraco's **Translation** section, which on a new site are Administrators and Translators. To
let more people edit, add the section to their user group in **Users > User groups**.

### Umbraco's Translation section

Umbraco's own **Translation** section holds only the Dictionary. On a site without Dictionary items
it is a second, empty translation section next to App Translations, so the package hides it while
the Dictionary is empty. As soon as the site has a Dictionary item, the section shows again.

To show it on a site without Dictionary items, for example to start adding some, turn off **Hide
while the Dictionary is empty** in **Settings > App translations**. The section comes back at once.

Hiding it changes nothing in the user groups: the package leaves it out of the backoffice, so
uninstalling the package brings it back.

## AI suggestions

With [Umbraco.AI](https://docs.umbraco.com/umbraco-ai) set up, the editor can translate a message
from the language you are comparing against. It can also improve, simplify or shorten the text in
the field, or fix its spelling.

Suggestions need Umbraco.AI 17.5 or later, which needs Umbraco 17.5 or later. This package does not
install Umbraco.AI for you. It comes with the provider package you install in step 1. Until the
site has it, the AI assistant card says what to install.

1. Install an Umbraco.AI provider package for your Umbraco version, for example `Umbraco.AI.OpenAI`.
2. In the **AI** section, add a connection and a chat profile. Put brand voice and tone on the
   profile as contexts. They apply to every suggestion.
3. In **Settings > App translations**, turn on **Offer in the editor** in the AI assistant card and
   choose the profile. You can also change the instructions sent with every request there.

The assistant is off until an administrator turns it on, because it sends the site's text to an AI
provider.

A suggestion only fills the field, and the site does not change until you save. The editor checks
every suggestion the same way it checks a save. If a reply drops or renames a placeholder, the
assistant sends it back once with the reason, and refuses it if the second reply is no better.

## Using the translations in your application

To use the custom text, fetch it for a language and namespace, then merge it over the messages your
application already bundles.

```http
GET /umbraco/delivery/api/v1/app-translations/overrides?locale=da-DK&namespace=website
```

If your application does not bundle its own text, fetch every message instead: the editor's text
where an editor changed it, and your source's text everywhere else.

```http
GET /umbraco/delivery/api/v1/app-translations/all?locale=da-DK&namespace=website
```

Repeat `namespace` to fetch several namespaces, or leave it out to fetch every namespace. Each is then
a top-level key holding its own messages.

Umbraco keeps each response in memory until the translations change, on every server. Clients and
CDNs may keep it for 60 seconds; `AppTranslations:Delivery:MaxAgeSeconds` changes that,
and `0` makes them check on every request. Every response carries an ETag, so a client that sends
`If-None-Match` gets `304 Not Modified` until something changes.

The endpoints are public until you set `AppTranslations:Delivery:ApiKey`. With a key set,
a request needs it in an `Api-Key` header or as a bearer token, and gets `401` without it. **Settings > App translations** lists the exact URL for each language and
namespace.

Messages are served in the syntax their source declares. Add `format=icu` (next-intl, react-intl,
Lingui) or `format=i18next` (i18next, react-i18next) to have Umbraco refuse anything your
application would misread. For i18next, set the source's message syntax to **i18next JSON v4**. The
output keeps nested keys, `{{variable}}` interpolation, context keys and plural suffixes such as
`_one`, `_other` and `_ordinal_one`. Umbraco never converts text between ICU and i18next syntax, so
keep each namespace to one syntax. Umbraco refuses to serve a namespace that mixes them until you
split it.

## Keeping things in sync

### Syncing a source

Umbraco reads a source when someone presses **Sync now** in **Settings > App translations**, which also
keeps the history of every sync. To sync as part of a deployment, use the endpoint below.

### Syncing from a deployment

A deployment can sync a source by its identifier, without a backoffice login. The endpoint is off
until you give your pipeline a way in: a secret, or an authorization policy, such as one that checks
the OIDC token its platform already issues. You can configure both and move pipelines over one at a
time.

With a secret, use a long random value; anyone who has it can start a sync.

```sh
AppTranslations__DeploymentSync__ApiKey=replace-with-a-long-random-value
```

Give your deployment pipeline the same secret, and have it call the endpoint once the new translation
files are live.

```sh
curl --fail-with-body \
  --request POST \
  --header "Authorization: Bearer $UMBRACO_TRANSLATIONS_SYNC_KEY" \
  "$UMBRACO_URL/umbraco/app-translations/api/v1/sources/website/sync"
```

Replace `website` with the source identifier from the source's **Advanced** settings. The request
waits for the sync to finish and returns how many messages it added, changed and removed.

Instead of a secret, a pipeline on GitHub Actions, GitLab CI or a cloud platform can send the
short-lived OIDC token its platform gives it. App Translations doesn't check the token itself: you
name an ASP.NET Core authorization policy, and any request that passes it can start a sync.
[API OIDC](https://api-oidc.thebuilder.dk/), a separate package, provides one. Install it, trust
your pipeline's tokens, and name its `TheBuilder.Oidc` policy:

```json
{
  "ApiOidc": {
    "Issuers": [{ "Preset": "GitHubActions", "Owner": "acme", "Repositories": ["acme/site"] }]
  },
  "AppTranslations": {
    "DeploymentSync": { "AuthorizationPolicy": "TheBuilder.Oidc" }
  }
}
```

The pipeline sends the token in the `X-Oidc-Token` header. If API OIDC also trusts your frontends,
they could start a sync too. [With an OIDC token](https://app-translations.thebuilder.dk/syncing#with-an-oidc-token)
shows how to let only the pipeline in, and a GitHub Actions workflow.

| Response | Meaning                                                                    |
| -------- | -------------------------------------------------------------------------- |
| `200`    | Synced.                                                                    |
| `401`    | The secret or token is missing or wrong.                                   |
| `403`    | The token is valid, but the policy doesn't let it in.                      |
| `404`    | No source has that identifier.                                             |
| `409`    | The source is turned off, or already syncing.                              |
| `502`    | Umbraco could not fetch or read the translation files.                     |
| `503`    | Neither a secret nor a policy is configured, so the endpoint is off.       |

Use HTTPS, and keep the secret in the secret stores on both sides.

### Telling your application something changed

To drop a cached dictionary the moment it changes, add a webhook in **Settings > Webhooks** for the
**App translations updated** event (under **Other**). Umbraco handles the URL, headers, retries and the
request log as it does for any webhook.

The event fires once for every change that reaches the delivery endpoints.

| `change`          | When                                                     |
| ----------------- | -------------------------------------------------------- |
| `Synchronized`    | A sync added, changed or removed at least one message.   |
| `OverrideSaved`   | An editor saved custom text.                             |
| `OverrideRemoved` | An editor reverted custom text to the default.           |
| `SourceDeleted`   | Someone deleted a source, and everything it provided.    |

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

For a saved or reverted message, `message` names it. Its locale and namespace tell you exactly which
dictionary to purge. For a sync, `synchronization` holds the revision and the added, changed and
removed counts. Umbraco does not announce a sync that found nothing new.

Umbraco sends webhooks only when `Umbraco:CMS:Webhook:Enabled` is on, which it is by default. In a
load-balanced setup, Umbraco sends webhooks only from the Single or SchedulingPublisher server.

## Configuration

| Setting                                                       | Default | Purpose                                                                                              |
| ------------------------------------------------------------- | ------- | ---------------------------------------------------------------------------------------------------- |
| `AppTranslations:SourceSecurity:AllowPrivateNetworkEndpoints` | `false` | Let sources read from private networks and `localhost`.                                              |
| `AppTranslations:DeploymentSync:ApiKey`                       | none    | Turns on deployment-triggered sync with a secret. A long random value, without spaces.               |
| `AppTranslations:DeploymentSync:AuthorizationPolicy`          | none    | Turns on deployment-triggered sync for requests that satisfy this ASP.NET Core authorization policy. |
| `AppTranslations:Delivery:ApiKey`                             | none    | Requires a key for the delivery endpoints, which are public without one.                             |
| `AppTranslations:Delivery:MaxAgeSeconds`                      | `60`    | How long clients and CDNs may keep a delivery response. `0` makes them check every time.             |

Source headers read their values from the setting names you give them.

## Documentation

The full documentation is at [app-translations.thebuilder.dk](https://app-translations.thebuilder.dk/).

- [Quickstart](https://app-translations.thebuilder.dk/quickstart): install the package and serve the first edit to your application.
- [Sources](https://app-translations.thebuilder.dk/sources): connect Umbraco to your translation files.
- [Editing translations](https://app-translations.thebuilder.dk/editing): find, change and review messages.
- [Delivery API](https://app-translations.thebuilder.dk/delivery-api): fetch the custom text and merge it into next-intl or i18next.
- [Syncing](https://app-translations.thebuilder.dk/syncing): sync from a deployment and react to changes with a webhook.
- [Troubleshooting](https://app-translations.thebuilder.dk/troubleshooting): the problems people run into first.

## Development

The repository includes a sample site with English and Danish messages to sync against.

```sh
pnpm install
pnpm build
dotnet run --project samples/TheBuilder.AppTranslations.Example
```

The sample runs at `https://localhost:44389` and installs itself. Its backoffice login is in
`samples/TheBuilder.AppTranslations.Example/appsettings.json`. Add a source with
`https://localhost:44389/sample/messages/{locale}.json` for ICU messages, or
`https://localhost:44389/sample/i18next/{locale}.json` for i18next JSON v4. The sample already
allows private-network sources.

Run these before opening a pull request.

```sh
dotnet test TheBuilder.AppTranslations.slnx
pnpm check   # type check, lint and dead-code check
pnpm test
pnpm build
```

`pnpm fix` applies formatting and the safe lint fixes. `src/TheBuilder.AppTranslations/Client/CLAUDE.md`
describes the client's conventions.

Generate the backoffice API client from a running sample.

```sh
pnpm --dir src/TheBuilder.AppTranslations/Client generate-client -- https://localhost:44389/umbraco/swagger/thebuilder-app-translations/swagger.json
```

The sample documents the deployment sync API at
`/umbraco/swagger/thebuilder-app-translations-automation/swagger.json`.

The documentation site lives in `apps/docs` and is built with Blume. `pnpm docs:dev` serves it
with hot reload, and `pnpm docs:build` builds it as CI does. With the sample running,
`pnpm docs:screenshots` retakes the backoffice screenshots the docs use; set `UMBRACO_URL` if the
sample is not at `https://localhost:44389`.

`THIRD-PARTY-NOTICES.md` lists the npm packages the backoffice bundle includes, with their licenses.
After changing the client's dependencies, run `pnpm build` and then `pnpm notices` to regenerate it;
CI fails while it is out of date.

[RELEASE.md](RELEASE.md) describes how to publish a release.

## License

MIT. See [LICENSE](LICENSE).
