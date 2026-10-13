import { ReactHostElement } from "../bridge/react-host.element.js";
import { EditorApp } from "./app/editor-app.js";

class TranslationsEditorHostElement extends ReactHostElement {
  protected component = EditorApp;
}

export default TranslationsEditorHostElement;

customElements.define("thebuilder-app-translations-editor", TranslationsEditorHostElement);

declare global {
  interface HTMLElementTagNameMap {
    "thebuilder-app-translations-editor": TranslationsEditorHostElement;
  }
}
