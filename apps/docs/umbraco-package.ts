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
  compatibility: { umbraco: ">=17.5 <18", dotnet: ">=10" },
  status: "preview",
  categories: ["Translations", "Headless"],
});

export const blurPlaceholderPackage = defineUmbracoPackage({
  id: "thebuilder.blurplaceholder",
  name: "Blur Placeholder",
  summary:
    "Generate WebP, BlurHash, or ThumbHash placeholders for Umbraco Image media and deliver one frontend-ready string.",
  links: {
    docs: "https://blur.thebuilder.dk/",
    nuget: "https://www.nuget.org/packages/TheBuilder.BlurPlaceholder",
    marketplace: "https://marketplace.umbraco.com/package/thebuilder.blurplaceholder",
    github: "https://github.com/thebuilder/blur-placeholder",
  },
  logo: "/ecosystem/blur-placeholder.svg",
  compatibility: { umbraco: ">=17.1 <19", dotnet: ">=10" },
  status: "stable",
  categories: ["Developer Tools", "Media"],
});

export const aiImageDisclosurePackage = defineUmbracoPackage({
  id: "thebuilder.aiimagedisclosure",
  name: "AI Image Disclosure",
  summary:
    "Check Umbraco images for signed C2PA claims of AI use. An OpenAI watermark check is optional.",
  links: {
    docs: "https://ai-image-disclosure.thebuilder.dk",
    nuget: "https://www.nuget.org/packages/TheBuilder.AIImageDisclosure",
    marketplace: "https://marketplace.umbraco.com/package/thebuilder.aiimagedisclosure",
    github: "https://github.com/thebuilder/umbraco-ai-image-disclosure",
  },
  logo: "/ecosystem/ai-image-disclosure.svg",
  compatibility: { umbraco: ">=17.1 <19", dotnet: ">=10" },
  status: "preview",
  categories: ["Artificial Intelligence", "Editor Tools"],
});

export const webAnalyticsPackage = defineUmbracoPackage({
  id: "thebuilder.webanalytics",
  name: "Web Analytics",
  summary:
    "Connect analytics providers and bring site-wide and page-level insights into the Umbraco backoffice.",
  links: {
    docs: "https://web-analytics.thebuilder.dk/",
    nuget: "https://www.nuget.org/packages/TheBuilder.WebAnalytics",
    marketplace: "https://marketplace.umbraco.com/package/thebuilder.webanalytics",
    github: "https://github.com/thebuilder/web-analytics",
  },
  logo: "/ecosystem/web-analytics.svg",
  compatibility: { umbraco: ">=17.1 <19", dotnet: ">=10" },
  status: "stable",
  categories: ["Analytics", "Editor Tools"],
});

export const ecosystemPackages = [
  appTranslationsPackage,
  blurPlaceholderPackage,
  aiImageDisclosurePackage,
  webAnalyticsPackage,
];
