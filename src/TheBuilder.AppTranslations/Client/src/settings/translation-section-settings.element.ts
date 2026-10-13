// biome-ignore-all lint/suspicious/noUnnecessaryConditions: Lit's @state decorator assigns these fields from outside the class, so the initializer is a default and not the only value they ever hold. The rule reads the initializer's literal type and calls every check on them constant.

import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  css,
  customElement,
  html,
  LitElement,
  live,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import type { UUIToggleElement } from "@umbraco-cms/backoffice/external/uui";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { api } from "../api/generated/client.js";
import type { TranslationSection } from "../api/generated/models.js";
import { setTranslationSectionShown } from "../translation-section.js";

/**
 * Whether Umbraco's own Translation section is hidden while the site has no Dictionary items.
 *
 * One switch, saved when it changes, so the card keeps its own state rather than handing a draft to
 * the dashboard the way the assistant's form does. The header follows the saved answer at once.
 */
@customElement("thebuilder-app-translations-translation-section-settings")
class TranslationSectionSettingsElement extends UmbElementMixin(LitElement) {
  @state() private _section?: TranslationSection;
  @state() private _saving = false;

  #notification?: typeof UMB_NOTIFICATION_CONTEXT.TYPE;

  constructor() {
    super();
    this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
      this.#notification = context;
    });
  }

  connectedCallback(): void {
    super.connectedCallback();
    api.translationSection().then(
      (section) => {
        this._section = section;
      },
      () => {
        // Not shown at all rather than shown wrong: the dashboard's other cards still work.
      }
    );
  }

  async #save(hide: boolean): Promise<void> {
    this._saving = true;
    try {
      const section = await api.saveTranslationSection({ hideWhileDictionaryIsEmpty: hide });
      this._section = section;
      setTranslationSectionShown(!section.hidden);
    } catch (error) {
      // The switch already moved. Render again so live() puts it back where the server still is.
      this.requestUpdate();
      this.#notification?.peek("danger", {
        data: {
          headline: "Could not save the Translation section setting",
          message: error instanceof Error ? error.message : String(error),
        },
      });
    } finally {
      this._saving = false;
    }
  }

  render() {
    const section = this._section;
    if (!section) {
      return null;
    }
    return html`<uui-box headline="Umbraco's Translation section" headline-variant="h2">
      <uui-toggle
        slot="header-actions"
        label="Hide Umbraco's Translation section while the site has no Dictionary items"
        .checked=${live(section.hideWhileDictionaryIsEmpty)}
        ?disabled=${this._saving}
        @change=${(event: Event) => this.#save((event.target as UUIToggleElement).checked)}>
        Hide while the Dictionary is empty
      </uui-toggle>
      <p>
        Umbraco's Translation section holds only the Dictionary. Without Dictionary items it is a
        second, empty translation section next to App Translations.
      </p>
      <p class="status">${this.#status(section)}</p>
    </uui-box>`;
  }

  #status(section: TranslationSection): string {
    if (section.hidden) {
      return "Hidden: the site has no Dictionary items.";
    }
    if (section.hasDictionaryItems) {
      return "Shown: the site has Dictionary items, so the section stays visible.";
    }
    return "Shown.";
  }

  static styles = [
    UmbTextStyles,
    css`
      :host { display: block; }
      uui-box { --uui-box-default-padding: var(--uui-size-space-4) var(--uui-size-space-5); }
      p { color: var(--uui-color-text-alt); margin: 0; max-inline-size: 70ch; }
      .status { color: var(--uui-color-text); font-weight: 700; margin-block-start: var(--uui-size-space-3); }
    `,
  ];
}

declare global {
  interface HTMLElementTagNameMap {
    "thebuilder-app-translations-translation-section-settings": TranslationSectionSettingsElement;
  }
}
