---
title: App Translations for Umbraco
description: Let editors change the text in your application from the Umbraco backoffice, without a code change or a deploy.
sidebar:
  label: Overview
seo:
  image: /og/overview.png
---

App Translations lets editors change the text in your application from the Umbraco backoffice, without a code change or a deploy.

Your application keeps its own translation files. Umbraco reads them, editors change the messages that need changing, and your application fetches those changes and lays them over its own text.

- **The application owns the messages.** Keys, the default text, the message syntax and the placeholders all come from the application's files, and stay there.
- **Umbraco owns the edits.** Editors write custom text for any message in any language, and Umbraco records who last changed each one, and when.
- **The application decides what to show.** It merges the custom text over its own, or asks Umbraco for the complete dictionary.

A site can also translate into [languages the application does not ship](/editing#languages-your-application-does-not-ship). Umbraco serves text written for such a language as the whole message, because the application has nothing to merge it over.

## What it reads

The package reads nested JSON, as next-intl and i18next write it, and next-intl's gettext PO files. Messages can use ICU syntax, as next-intl and FormatJS use it, i18next JSON v4, or plain text. The editor checks every change against that syntax, so an edit cannot break a placeholder or a plural.

## Requirements

- Umbraco CMS 17.1 through 18.x, on .NET 10.
- For [AI suggestions](/ai-suggestions) only, Umbraco.AI, which needs CMS 17.5 or later on 17.x, or CMS 18.x.
- Translation files your Umbraco site can fetch over HTTP, one file per language.

## Where to go next

- [Quickstart](/quickstart) installs the package and gets a first message on screen.
- [Sources](/sources) covers URLs, message syntax, namespaces and authenticated sources.
- [Delivery API](/delivery-api) shows how your application fetches the custom text, with next-intl and i18next examples.
- [Troubleshooting](/troubleshooting) covers the problems people run into first.
