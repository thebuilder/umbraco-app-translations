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
      endpoint("da-DK", "website", "next-intl"),
      endpoint("da-DK", "labels", "next-intl"),
      endpoint("da-DK", "labels", "i18next-v4"),
      endpoint("en-US", "website", "i18next-v4"),
    ];

    expect(allNamespaceFormats(endpoints, [], "da-DK")).toEqual(["next-intl"]);
  });

  it("offers none when the namespaces need different formats", () => {
    const endpoints = [
      endpoint("da-DK", "website", "next-intl"),
      endpoint("da-DK", "app", "i18next-v4"),
    ];

    expect(allNamespaceFormats(endpoints, [], "da-DK")).toEqual([]);
  });

  it("offers none when a namespace in the language mixes syntaxes", () => {
    // The mixed namespace has no endpoint, only a conflict, and the API refuses any request
    // that includes it.
    const endpoints = [endpoint("da-DK", "website", "next-intl")];
    const conflicts: OutputConflict[] = [
      { locale: "da-DK", namespace: "legacy", messageFormats: ["Icu", "I18NextV4"] },
    ];

    expect(allNamespaceFormats(endpoints, conflicts, "da-DK")).toEqual([]);
    expect(allNamespaceFormats(endpoints, conflicts, "en-US")).toEqual([]);
  });

  it("is not thrown by the same endpoint listed twice", () => {
    const endpoints = [
      endpoint("da-DK", "website", "next-intl"),
      endpoint("da-DK", "website", "next-intl"),
      endpoint("da-DK", "labels", "i18next-v4"),
    ];

    expect(allNamespaceFormats(endpoints, [], "da-DK")).toEqual([]);
  });

  it("offers none for a language with no endpoints", () => {
    expect(allNamespaceFormats([endpoint("da-DK", "website", "next-intl")], [], "sv-SE")).toEqual(
      []
    );
  });
});

describe("allNamespacesFormat", () => {
  it("keeps the chosen format when it still serves every namespace", () => {
    expect(allNamespacesFormat(["next-intl", "i18next-v4"], "i18next-v4")).toBe("i18next-v4");
  });

  it("falls back to the first format otherwise", () => {
    expect(allNamespacesFormat(["next-intl"], "i18next-v4")).toBe("next-intl");
    expect(allNamespacesFormat([], undefined)).toBeUndefined();
  });
});
