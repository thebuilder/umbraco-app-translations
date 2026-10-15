import { describe, expect, it } from "vitest";
import type { OutputConflict, OutputEndpoint } from "../../api/generated/models.js";
import { allNamespaceFormats, allNamespacesFormat } from "./all-namespaces.js";

const endpoint = (
  locale: string,
  namespace: string,
  format: OutputEndpoint["format"]
): OutputEndpoint => ({ locale, namespace, format });

describe("allNamespaceFormats", () => {
  it("offers the formats every namespace in the language can be served in", () => {
    const endpoints = [
      endpoint("da-DK", "website", "icu"),
      endpoint("da-DK", "labels", "icu"),
      endpoint("da-DK", "labels", "i18next"),
      endpoint("en-US", "website", "i18next"),
    ];

    expect(allNamespaceFormats(endpoints, [], "da-DK")).toEqual(["icu"]);
  });

  it("offers none when the namespaces need different formats", () => {
    const endpoints = [endpoint("da-DK", "website", "icu"), endpoint("da-DK", "app", "i18next")];

    expect(allNamespaceFormats(endpoints, [], "da-DK")).toEqual([]);
  });

  it("offers none when a namespace in the language mixes syntaxes", () => {
    // The mixed namespace has no endpoint, only a conflict, and the API refuses any request
    // that includes it.
    const endpoints = [endpoint("da-DK", "website", "icu")];
    const conflicts: OutputConflict[] = [
      { locale: "da-DK", namespace: "legacy", messageFormats: ["Icu", "I18NextV4"] },
    ];

    expect(allNamespaceFormats(endpoints, conflicts, "da-DK")).toEqual([]);
    expect(allNamespaceFormats(endpoints, conflicts, "en-US")).toEqual([]);
  });

  it("is not thrown by the same endpoint listed twice", () => {
    const endpoints = [
      endpoint("da-DK", "website", "icu"),
      endpoint("da-DK", "website", "icu"),
      endpoint("da-DK", "labels", "i18next"),
    ];

    expect(allNamespaceFormats(endpoints, [], "da-DK")).toEqual([]);
  });

  it("offers none for a language with no endpoints", () => {
    expect(allNamespaceFormats([endpoint("da-DK", "website", "icu")], [], "sv-SE")).toEqual([]);
  });
});

describe("allNamespacesFormat", () => {
  it("keeps the chosen format when it still serves every namespace", () => {
    expect(allNamespacesFormat(["icu", "i18next"], "i18next")).toBe("i18next");
  });

  it("falls back to the first format otherwise", () => {
    expect(allNamespacesFormat(["icu"], "i18next")).toBe("icu");
    expect(allNamespacesFormat([], undefined)).toBeUndefined();
  });
});
