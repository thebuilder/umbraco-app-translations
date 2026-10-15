import type { OutputConflict, OutputEndpoint } from "../../api/generated/models.js";
import type { DeliveryFormat } from "./source-form.js";

/**
 * The namespace select's value for every namespace. The delivery API reads a blank namespace as
 * every namespace, so a blank one is never a namespace of its own. Not the empty string: uui-select
 * gives its own disabled placeholder option that value.
 */
export const allNamespaces = " ";

/**
 * The formats every namespace in a language can be served in at once. A request for every
 * namespace needs one of them: a namespace in another syntax makes the whole response a 409.
 *
 * A namespace that mixes syntaxes has no endpoint at all, only a conflict, so it would be missed by
 * looking at the endpoints alone. The API still includes it in a request for every namespace and
 * refuses that request, so a language with a conflict has no format for every namespace.
 */
export const allNamespaceFormats = (
  endpoints: readonly OutputEndpoint[],
  conflicts: readonly OutputConflict[],
  locale: string
): DeliveryFormat[] => {
  if (conflicts.some((conflict) => conflict.locale === locale)) {
    return [];
  }
  const forLocale = endpoints.filter((endpoint) => endpoint.locale === locale);
  const namespaces = new Set(forLocale.map((endpoint) => endpoint.namespace));
  return [...new Set(forLocale.map((endpoint) => endpoint.format))].filter(
    (format) =>
      new Set(
        forLocale
          .filter((endpoint) => endpoint.format === format)
          .map((endpoint) => endpoint.namespace)
      ).size === namespaces.size
  );
};

/** Keeps the format already chosen when every namespace can be served in it, or picks the first. */
export const allNamespacesFormat = (
  formats: readonly DeliveryFormat[],
  current: DeliveryFormat | undefined
): DeliveryFormat | undefined =>
  current !== undefined && formats.includes(current) ? current : formats[0];
