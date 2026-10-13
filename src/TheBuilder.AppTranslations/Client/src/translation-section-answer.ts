import { ApiError } from "./api/errors.js";

/** Whether Umbraco's Translation section shows, and whether the next load may start from that. */
export interface TranslationSectionAnswer {
  remember: boolean;
  shown: boolean;
}

/**
 * The answer for this load. The server's answer is remembered. So is a refusal, from a user who
 * cannot reach this package's API: that user cannot open App Translations either, so hiding
 * Translation would leave them nothing, and starting the next load shown spares them a late tab.
 * Any other failure shows the section for this load only, because remembering it would start the
 * next load shown on a site that hides it, which is the flash this exists to prevent.
 */
export const askTranslationSection = async (
  ask: () => Promise<{ hidden: boolean }>
): Promise<TranslationSectionAnswer> => {
  try {
    return { shown: !(await ask()).hidden, remember: true };
  } catch (error) {
    return { shown: true, remember: error instanceof ApiError && error.isForbidden };
  }
};

const storageKey = "thebuilder-app-translations:translation-section-shown";

/** What this browser saw last time. Nothing remembered, or no storage, starts hidden. */
export const rememberedShown = (storage: Pick<Storage, "getItem"> | undefined): boolean => {
  try {
    return storage?.getItem(storageKey) === "true";
  } catch {
    return false;
  }
};

export const rememberShown = (
  storage: Pick<Storage, "setItem"> | undefined,
  shown: boolean
): void => {
  try {
    storage?.setItem(storageKey, String(shown));
  } catch {
    // Without storage, the next load starts hidden again and the server corrects it, as on a first visit.
  }
};
