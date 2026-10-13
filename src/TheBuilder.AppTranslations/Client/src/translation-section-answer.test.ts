import { describe, expect, it } from "vitest";
import { ApiError } from "./api/errors.js";
import {
  askTranslationSection,
  rememberedShown,
  rememberShown,
} from "./translation-section-answer.js";

const memoryStorage = () => {
  const values = new Map<string, string>();
  return {
    getItem: (key: string) => values.get(key) ?? null,
    setItem: (key: string, value: string) => {
      values.set(key, value);
    },
  };
};

describe("askTranslationSection", () => {
  it("hides the section when the server says hidden, and remembers it", async () => {
    expect(await askTranslationSection(async () => ({ hidden: true }))).toEqual({
      shown: false,
      remember: true,
    });
  });

  it("shows the section when the server says shown, and remembers it", async () => {
    expect(await askTranslationSection(async () => ({ hidden: false }))).toEqual({
      shown: true,
      remember: true,
    });
  });

  it("shows the section to a user the API refuses, and remembers it", async () => {
    const refused = () => Promise.reject(new ApiError(403, "Forbidden"));
    expect(await askTranslationSection(refused)).toEqual({ shown: true, remember: true });
  });

  it.each([0, 401, 500, 503])(
    "shows the section after a %i without remembering it",
    async (status) => {
      const failed = () => Promise.reject(new ApiError(status, "Failed"));
      expect(await askTranslationSection(failed)).toEqual({ shown: true, remember: false });
    }
  );

  it("shows the section after a network failure without remembering it", async () => {
    const thrown = () => Promise.reject(new TypeError("Failed to fetch"));
    expect(await askTranslationSection(thrown)).toEqual({ shown: true, remember: false });
  });
});

describe("remembered answer", () => {
  it("starts hidden when nothing is remembered or there is no storage", () => {
    expect(rememberedShown(memoryStorage())).toBe(false);
    expect(rememberedShown(undefined)).toBe(false);
  });

  it("starts from what was remembered", () => {
    const storage = memoryStorage();
    rememberShown(storage, true);
    expect(rememberedShown(storage)).toBe(true);
    rememberShown(storage, false);
    expect(rememberedShown(storage)).toBe(false);
  });

  it("starts hidden when storage throws", () => {
    const blocked = {
      getItem: () => {
        throw new DOMException("Blocked", "SecurityError");
      },
    };
    expect(rememberedShown(blocked)).toBe(false);
  });
});
