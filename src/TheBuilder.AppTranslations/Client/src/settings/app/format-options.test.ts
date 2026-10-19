import { describe, expect, it } from "vitest";
import { parseSyncInterval, syncIntervalLabel, syncIntervalOptions } from "./format-options.js";

describe("sync schedule options", () => {
  it("offers no schedule first, and selects it when the source has none", () => {
    const options = syncIntervalOptions(null);

    expect(options[0]).toEqual({
      name: "Only when synced by hand or by a deployment",
      value: "",
      selected: true,
    });
    expect(options.filter((option) => option.selected)).toHaveLength(1);
  });

  it("keeps a schedule the list does not offer, so saving does not lose it", () => {
    const options = syncIntervalOptions(90);

    expect(options.at(-1)).toEqual({ name: "Every 90 minutes", value: "90", selected: true });
  });

  it("names whole hours, days and weeks", () => {
    expect(syncIntervalLabel(60)).toBe("Every hour");
    expect(syncIntervalLabel(360)).toBe("Every 6 hours");
    expect(syncIntervalLabel(1440)).toBe("Every day");
    expect(syncIntervalLabel(10_080)).toBe("Every week");
  });

  it("reads the empty choice as no schedule", () => {
    expect(parseSyncInterval("")).toBeNull();
    expect(parseSyncInterval("1440")).toBe(1440);
  });
});
