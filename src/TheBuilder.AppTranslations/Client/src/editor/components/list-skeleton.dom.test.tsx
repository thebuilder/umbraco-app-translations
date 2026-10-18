import { cleanup, render } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { LocaleFacet, MessageKey } from "../../api/generated/models.js";
import styles from "../../styles.css?inline";
import { defaultFilters } from "../state/filters.js";
import { KeyGrid } from "./key-grid.js";
import { ListState } from "./list-state.js";

afterEach(() => {
  cleanup();
  document.body.replaceChildren();
});

const locale = (code: string, name: string): LocaleFacet => ({
  code,
  name,
  isDefault: code === "en",
  isConfigured: true,
  messageCount: 10,
  overriddenCount: 0,
  needsReviewCount: 0,
  absentKeyCount: 0,
});

const key = (name: string): MessageKey => ({
  sourceId: "s1",
  namespace: "website",
  key: name,
  format: "Icu",
  arguments: {},
  cells: {
    da: {
      id: `da-${name}`,
      locale: "da",
      defaultValue: "Tekst",
      overrideValue: null,
      hasOverride: false,
      needsReview: false,
      truncated: false,
      state: "Default",
      version: null,
      updatedAt: null,
      updatedBy: null,
    },
  },
  coverage: {},
  matchedLocales: [],
});

/**
 * A box the size of the board, inside a shadow root carrying the real stylesheet: the styles are
 * scoped to `:host`, and what is under test is where the stylesheet puts things.
 */
const board = () => {
  const host = document.createElement("div");
  host.style.cssText = "display: block; inline-size: 1000px; block-size: 600px;";
  document.body.append(host);
  const root = host.attachShadow({ mode: "open" });
  const sheet = new CSSStyleSheet();
  sheet.replaceSync(styles);
  root.adoptedStyleSheets = [sheet];
  const container = document.createElement("div");
  container.className = "board";
  container.style.cssText = "display: flex; flex-direction: column; block-size: 100%;";
  root.append(container);
  return container;
};

/** Each row's top and height, measured from the top of the board it is drawn in. */
const rowsIn = (container: HTMLElement, selector: string) => {
  const origin = container.getBoundingClientRect().top;
  return [...container.querySelectorAll(selector)].slice(0, 8).map((row) => {
    const box = row.getBoundingClientRect();
    return { top: Math.round(box.top - origin), height: Math.round(box.height) };
  });
};

describe("the loading outline", () => {
  it("draws each row exactly where the real row lands", () => {
    const loading = board();
    render(
      <ListState
        canManageSources={false}
        filtered={false}
        onClear={vi.fn()}
        onRetry={vi.fn()}
        state={{ kind: "loading" }}
        term=""
      />,
      { container: loading }
    );

    const loaded = board();
    const keys = Array.from({ length: 8 }, (_, index) => key(`key${index}`));
    render(
      <KeyGrid
        filters={{ ...defaultFilters, locale: "da", referenceLocale: null }}
        hasMore={false}
        keys={keys}
        loadingMore={false}
        locales={[locale("da", "Danish"), locale("en", "English")]}
        namespaceCount={1}
        onLoadMore={vi.fn()}
        onSelect={vi.fn()}
        selected={undefined}
        total={keys.length}
      />,
      { container: loaded }
    );

    const real = rowsIn(loaded, ".grid__body .grid__row");
    expect(real).toHaveLength(8);
    expect(rowsIn(loading, ".skeleton-row")).toEqual(real);

    // And each bar starts where the words in its column do: the key, then the language.
    const left = (element: Element | null) =>
      Math.round(element?.getBoundingClientRect().left ?? -1);
    const bars = loading.querySelector(".skeleton-row")?.querySelectorAll(".skeleton") ?? [];
    const firstRow = loaded.querySelector(".grid__body .grid__row");
    expect([left(bars[0] ?? null), left(bars[1] ?? null)]).toEqual([
      left(firstRow?.querySelector(".grid__cell--key") ?? null),
      left(firstRow?.querySelector(".grid__cell--lead") ?? null),
    ]);
  });
});
