import { defineConfig } from "blume";
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
      { type: "filesystem", root: "content" },
      // GitHub Releases are the changelog, as RELEASE.md says, so the notes are written once.
      {
        type: "custom",
        source: githubReleaseChangelogSource({
          owner: "thebuilder",
          repo: "umbraco-app-translations",
        }),
      },
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
  deployment: {
    output: "static",
    site: "https://app-translations.thebuilder.dk",
  },
  seo: {
    // Package-owned cards are generated into public/og before Blume builds.
    // Disable Blume's generated /og routes so it cannot overwrite them.
    og: { enabled: false },
  },
  theme: {
    accent: "#7557e8",
    fonts: {
      display: "inter-tight",
    },
  },
  ai: {
    llmsTxt: true,
  },
});
