import { defineOgConfig } from "@thebuilder/umbraco-docs/og";
import { fileURLToPath } from "node:url";

export default defineOgConfig({
  contentDir: fileURLToPath(new URL("./content", import.meta.url)),
  publicDir: fileURLToPath(new URL("./public", import.meta.url)),
  prefix: "/og",
  brand: "TheBuilder · App Translations",
  accent: "#7557e8",
  logo: "/logo-mark.svg",
  root: {
    title: "App Translations for Umbraco",
    description: "Let editors change your application's next-intl and i18next messages from Umbraco.",
  },
});
