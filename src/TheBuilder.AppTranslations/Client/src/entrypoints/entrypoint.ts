import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import type {
  UmbEntryPointOnInit,
  UmbEntryPointOnUnload,
} from "@umbraco-cms/backoffice/extension-api";
import { configureApi } from "../api/generated/client.js";
import { resetQueryCache } from "../bridge/query-cache-reset.js";
import { applyTranslationSectionSetting } from "../translation-section.js";

export const onInit: UmbEntryPointOnInit = (host) => {
  host.consumeContext(UMB_AUTH_CONTEXT, (authContext) => {
    const config = authContext?.getOpenApiConfiguration();
    configureApi({
      token: config?.token,
      baseUrl: config?.base ?? "",
      credentials: config?.credentials ?? "same-origin",
    });
    // The query cache outlives the editor element, so it has to be dropped whenever the identity
    // behind those requests changes. Otherwise a second user would see the first user's data.
    resetQueryCache();
    // After configureApi, so the question goes out with this identity's token.
    applyTranslationSectionSetting();
  });
};

export const onUnload: UmbEntryPointOnUnload = () => {
  configureApi({ baseUrl: "", credentials: "same-origin" });
  resetQueryCache();
};
