import { manifests as entrypointManifests } from "./entrypoints/manifest.js";
import { extensionManifests } from "./manifests.js";
import { conditionTranslationSection } from "./translation-section.js";

// When Umbraco evaluates the bundle, before the header renders, so that Umbraco's Translation
// section waits for the package's answer instead of showing first and disappearing after.
conditionTranslationSection();

export const manifests: UmbExtensionManifest[] = [...entrypointManifests, ...extensionManifests];
