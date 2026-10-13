export const manifests: UmbExtensionManifest[] = [
  {
    type: "backofficeEntryPoint",
    alias: "TheBuilder.AppTranslations.EntryPoint",
    name: "App Translations API Entry Point",
    js: () => import("./entrypoint.js"),
  },
];
