import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type {
  UmbConditionConfigBase,
  UmbConditionControllerArguments,
  UmbExtensionCondition,
} from "@umbraco-cms/backoffice/extension-api";
import {
  UmbConditionBase,
  umbExtensionsRegistry,
} from "@umbraco-cms/backoffice/extension-registry";
import { UmbBooleanState } from "@umbraco-cms/backoffice/observable-api";
import { UMB_TRANSLATION_SECTION_ALIAS } from "@umbraco-cms/backoffice/translation";
import { api } from "./api/generated/client.js";
import {
  askTranslationSection,
  rememberedShown,
  rememberShown,
} from "./translation-section-answer.js";

const conditionAlias = "TheBuilder.AppTranslations.Condition.TranslationSectionShown";

const storage = (): Storage | undefined => {
  try {
    return localStorage;
  } catch {
    // Reading localStorage throws where site data is blocked. No storage means nothing remembered.
  }
};

/**
 * Whether Umbraco's Translation section shows, starting from what this browser saw last time.
 *
 * The answer comes from the server, which the package can only ask once the backoffice is running,
 * and the header renders before then. Starting from the last answer means the header is right from
 * its first frame unless the answer has changed since, and then the server's answer corrects it in
 * the same load: this is a condition, so it can change its mind, where an exclusion could not.
 * A browser with nothing remembered starts hidden, the case on a site without Dictionary items.
 */
const shown = new UmbBooleanState(rememberedShown(storage()));

export const setTranslationSectionShown = (value: boolean, remember = true): void => {
  shown.setValue(value);
  if (remember) {
    rememberShown(storage(), value);
  }
};

class TranslationSectionShownCondition
  extends UmbConditionBase<UmbConditionConfigBase>
  implements UmbExtensionCondition
{
  constructor(
    host: UmbControllerHost,
    args: UmbConditionControllerArguments<UmbConditionConfigBase>
  ) {
    super(host, args);
    this.observe(shown.asObservable(), (value) => {
      this.permitted = value;
    });
  }
}

/**
 * Puts Umbraco's Translation section behind the condition above. Called when Umbraco evaluates the
 * bundle, which is before the header renders the section, so the condition applies from the start.
 *
 * Nothing in the site's user groups changes, and nothing is unregistered: uninstalling the package
 * removes the condition with the bundle, and the section is back.
 */
export const conditionTranslationSection = (): void => {
  umbExtensionsRegistry.register({
    type: "condition",
    alias: conditionAlias,
    name: "Umbraco's Translation section is shown",
    api: TranslationSectionShownCondition,
  });
  umbExtensionsRegistry.appendCondition(UMB_TRANSLATION_SECTION_ALIAS, { alias: conditionAlias });
};

/**
 * Asks the server whether to show the Translation section: hidden only while the setting is on and
 * the site has no Dictionary items. Asked whenever the signed-in identity changes, since a different
 * user can have different access to this package's API. The settings card reports a change of
 * setting itself.
 */
export const applyTranslationSectionSetting = async (): Promise<void> => {
  const answer = await askTranslationSection(api.translationSection);
  setTranslationSectionShown(answer.shown, answer.remember);
};
