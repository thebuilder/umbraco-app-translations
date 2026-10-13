import type { ManifestDashboard } from "@umbraco-cms/backoffice/dashboard";
import type { ManifestSection, ManifestSectionView } from "@umbraco-cms/backoffice/section";

/** The package's own section. Matches `Constants.SectionAlias`, which user groups are granted. */
const sectionAlias = "TheBuilder.AppTranslations.Section";

const sectionManifest: ManifestSection = {
  type: "section",
  alias: sectionAlias,
  name: "App Translations Section",
  // Just after Umbraco's Translation section (400), where a site that keeps it will look.
  weight: 390,
  meta: { label: "App Translations", pathname: "app-translations" },
  conditions: [{ alias: "Umb.Condition.SectionUserPermission", match: sectionAlias }],
};

const editorManifest: ManifestSectionView = {
  type: "sectionView",
  alias: "TheBuilder.AppTranslations.SectionView",
  name: "App Translations Editor",
  element: () => import("./editor/editor-host.element.js"),
  meta: { label: "App translations", pathname: "overview", icon: "icon-globe" },
  conditions: [{ alias: "Umb.Condition.SectionAlias", match: sectionAlias }],
};

const settingsManifest: ManifestDashboard = {
  type: "dashboard",
  alias: "TheBuilder.AppTranslations.SettingsDashboard",
  name: "App Translations Settings",
  element: () => import("./settings/settings-dashboard.element.js"),
  meta: { label: "App translations", pathname: "app-translations" },
  conditions: [
    { alias: "Umb.Condition.SectionAlias", match: "Umb.Section.Settings" },
    { alias: "Umb.Condition.CurrentUser.IsAdmin" },
  ],
};

export const extensionManifests: UmbExtensionManifest[] = [
  sectionManifest,
  editorManifest,
  settingsManifest,
];
