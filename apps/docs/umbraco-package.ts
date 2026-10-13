import { defineUmbracoPackage } from "@thebuilder/umbraco-docs";

export const appTranslationsPackage = defineUmbracoPackage({
  id: "thebuilder.apptranslations",
  name: "App Translations",
  summary:
    "Let editors change your application's next-intl and i18next messages from Umbraco, without a code change or a deploy.",
  links: {
    docs: "https://app-translations.thebuilder.dk",
    nuget: "https://www.nuget.org/packages/TheBuilder.AppTranslations",
    marketplace: "https://marketplace.umbraco.com/package/thebuilder.apptranslations",
    github: "https://github.com/thebuilder/umbraco-app-translations",
  },
  logo: "/logo-mark.svg",
  compatibility: { umbraco: ">=17.1 <19", dotnet: ">=10" },
  status: "preview",
  categories: ["Translations", "Headless"],
});
