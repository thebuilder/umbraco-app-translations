import { docsTheme } from "@thebuilder/umbraco-docs";
import { defineConfig } from "blume";
import { vercel } from "blume/analytics";
import { custom, filesystem } from "blume/sources";
import { githubReleaseChangelogSource } from "./sources/github-releases";
import { appTranslationsPackage } from "./umbraco-package";

export default defineConfig({
  title: appTranslationsPackage.name,
  description: appTranslationsPackage.summary,
  logo: {
    image: "/logo-mark.svg",
    text: "App Translations",
  },
  github: {
    owner: "thebuilder",
    repo: "umbraco-app-translations",
    dir: "apps/docs",
  },
  content: {
    sources: [
      filesystem({ root: "content" }),
      // GitHub Releases are the changelog, as RELEASE.md says, so the notes are written once.
      custom(
        githubReleaseChangelogSource({
          owner: "thebuilder",
          repo: "umbraco-app-translations",
        }),
      ),
    ],
  },
  navigation: {
    // A custom landing page owns "/", so the Docs tab links to the overview
    // while keeping path "/" to stay highlighted across every docs route.
    tabs: [
      { label: "Docs", path: "/", href: "/overview" },
      { label: "Changelog", path: "/changelog", href: "/changelog" },
    ],
  },
  // The endpoint pages under content/endpoints show request samples against a placeholder site.
  api: {
    server: "https://umbraco.example.com",
  },
  deployment: {
    site: "https://app-translations.thebuilder.dk",
  },
  seo: {
    // Package-owned cards are generated into public/og before Blume builds.
    // Disable Blume's generated /og routes so it cannot overwrite them.
    og: { enabled: false },
  },
  analytics: [vercel()],
  // The docs family's shared accent and fonts, so every package site looks alike.
  theme: { ...docsTheme },
  agents: {
    llmsTxt: true,
  },
});
